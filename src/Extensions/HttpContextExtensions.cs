using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using SolidTUS.Builders;
using SolidTUS.Contexts;
using SolidTUS.Handlers;
using SolidTUS.Models;
using SolidTUS.ProtocolFlows;

namespace SolidTUS.Extensions;

/// <summary>
/// <see cref="HttpContext"/> extension methods for TUS protocol
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Create new tus creation context.
    /// </summary>
    /// <param name="context">The http context</param>
    /// <param name="fileId">The file id</param>
    /// <returns>A tus creation context</returns>
    /// <exception cref="InvalidOperationException">Thrown if missing tus creation context service</exception>
    public static TusCreationContextBuilder TusCreation(this HttpContext context, string fileId)
    {
        return new(fileId);
    }

    /// <summary>
    /// Start either:
    /// <para>
    /// - Create single resource metadata, which could include upload data
    /// </para>
    /// <para>
    /// - Create partial resource metadata
    /// </para>
    /// - Start merge partial resources into single file
    /// </summary>
    /// <param name="context">The http context</param>
    /// <param name="creationContext">The tus creation context</param>
    /// <returns>An awaitable task</returns>
    public static async Task StartCreationAsync(this HttpContext context, TusCreationContext creationContext)
    {
        if (context.RequestServices.GetService(typeof(ResourceCreationHandler)) is not ResourceCreationHandler resource)
        {
            throw new InvalidOperationException("Remember to register SolidTUS on program startup");
        }

        if (context.Items[TusResult.Name] is not TusResult tusResult)
        {
            throw new InvalidOperationException("Can only use this method in conjuction with either endpoint filter or action filter.");
        }

        resource.SetDetails(creationContext, tusResult);
        resource.SetPipeReader(context.Request.BodyReader);

        var hasLength = long.TryParse(context.Request.Headers[HeaderNames.ContentLength], out var contentLength);
        var isUpload = hasLength && contentLength > 0;
        var cancel = context.RequestAborted;

        var response = tusResult.PartialMode switch
        {
            PartialMode.None => await resource.CreateResourceAsync(isUpload, cancel),
            PartialMode.Partial => await resource.CreateResourceAsync(isUpload, cancel),
            PartialMode.Final => await resource.MergeFilesAsync(cancel),
            _ => throw new NotImplementedException(),
        };

        context.Items[CreationResultName] = response;
    }

    /// <summary>
    /// Get tus metadata if there are any
    /// </summary>
    /// <param name="context">The http context</param>
    /// <returns>A tus metadata dictionary</returns>
    public static IReadOnlyDictionary<string, string>? TusMetadata(this HttpContext context)
    {
        if (context.Items[TusResult.Name] is not TusResult request)
        {
            throw new InvalidOperationException("Can only use this method in conjuction with either endpoint filter or action filter.");
        }

        return request.Metadata;
    }

    /// <summary>
    /// Create new tus upload context builder
    /// </summary>
    /// <param name="context">The http context</param>
    /// <param name="fileId">The file id</param>
    /// <returns>A tus upload context builder</returns>
    public static TusUploadContextBuilder TusUpload(this HttpContext context, string fileId)
    {
        return new(fileId);
    }

    /// <summary>
    /// Start appending data from the client
    /// </summary>
    /// <param name="context">The http context</param>
    /// <param name="uploadContext">The tus upload settings</param>
    /// <returns>An awaitable task</returns>
    /// <exception cref="InvalidOperationException">Thrown when SolidTus is not registered on startup</exception>
    public static async Task StartAppendDataAsync(this HttpContext context, TusUploadContext uploadContext)
    {
        if (context.RequestServices.GetService(typeof(UploadHandler)) is not UploadHandler uploadHandler
        || context.RequestServices.GetService(typeof(UploadFlow)) is not UploadFlow uploadFlow)
        {
            throw new InvalidOperationException("Remember to register SolidTUS on program startup");
        }

        if (context.Items[TusResult.Name] is not TusResult tusResult)
        {
            throw new InvalidOperationException("Can only use this method in conjuction with either endpoint filter or action filter.");
        }

        var cancel = context.RequestAborted;
        var upload = await uploadHandler.HandleUploadAsync(context.Request.BodyReader, uploadContext, tusResult, cancel);

        // POST result handling
        var result = upload.Map(uploadFlow.PostUpload);
        if (result.TryGetError(out var error))
        {
            context.Response.StatusCode = error.StatusCode;
        }

        var headers = result.Match(s => s.ResponseHeaders, e => e.Headers);
        foreach (var (key, value) in headers)
        {
            context.Response.Headers.Append(key, value);
        }
    }

    internal static void SetErrorHeaders(this HttpContext context, HttpError error)
    {
        foreach (var (key, value) in error.Headers)
        {
            context.Response.Headers.Append(key, value);
        }
    }

    /// <summary>
    /// The result of the <see cref="StartCreationAsync(HttpContext, TusCreationContext)"/> stored in <see cref="HttpContext.Items"/>
    /// </summary>
    public const string CreationResultName = "__SolidTusCreationUploadInfo__";
}

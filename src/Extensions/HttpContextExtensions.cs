using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using SolidTUS.Contexts;
using SolidTUS.Handlers;
using SolidTUS.Models;

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
    public static TusCreationContext TusCreation(this HttpContext context, string fileId)
    {
        if (context.RequestServices.GetService(typeof(TusCreationContext)) is not TusCreationContext creation)
        {
            throw new UnreachableException();
        }

        creation.FileId = fileId;
        return creation;
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
        if (string.IsNullOrWhiteSpace(creationContext.UploadUrl))
        {
            throw new InvalidOperationException("Must provide an upload url to the TusUpload endpoint");
        }

        if (context.RequestServices.GetService(typeof(ResourceCreationHandler)) is not ResourceCreationHandler resource)
        {
            throw new UnreachableException();
        }

        if (context.Items[RequestContext.Name] is not RequestContext request)
        {
            throw new UnreachableException();
        }

        resource.SetDetails(creationContext, request);
        resource.SetPipeReader(context.Request.BodyReader);
        var hasLength = long.TryParse(context.Request.Headers[HeaderNames.ContentLength], out var contentLength);
        var isUpload = hasLength && contentLength > 0;
        var cancel = context.RequestAborted;

        UploadFileInfo? uploadInfo = request.PartialMode switch
        {
            PartialMode.None => await resource.CreateResourceAsync(isUpload, cancel),
            PartialMode.Partial => await resource.CreateResourceAsync(isUpload, cancel),
            PartialMode.Final => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
        };


        if (uploadInfo is not null)
        {
            context.Items[CreationResultName] = uploadInfo;
        }
    }

    /// <summary>
    /// The result of the <see cref="StartCreationAsync(HttpContext, TusCreationContext)"/> stored in <see cref="HttpContext.Items"/>
    /// </summary>
    public const string CreationResultName = "__SolidTusCreationUploadInfo__";
}

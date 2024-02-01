using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using SolidTUS.Extensions;
using SolidTUS.Functional.Models;
using SolidTUS.Handlers;
using SolidTUS.Models;
using SolidTUS.ProtocolFlows;

namespace SolidTUS.Contexts;

/// <summary>
/// Creation context for TUS
/// </summary>
public sealed record class TusCreationContext
{
    /// <summary>
    /// Instantiate a new object of <see cref="TusCreationContext"/>
    /// </summary>
    /// <param name="fileId">The file id</param>
    /// <param name="fileIdParameterName">The file id route parameter</param>
    /// <param name="routeName">The route name</param>
    /// <param name="routeValues">The extra optional route values</param>
    /// <param name="filename">The optional filename</param>
    /// <param name="directory">The optional directory</param>
    /// <param name="resourceCreatedCallback">The resource created callback</param>
    /// <param name="uploadFinishedCallback">The upload finished callback when using Creation-With-Upload</param>
    /// <param name="partialId">The partial id</param>
    /// <param name="allowMergeCallback">The allow merge callback</param>
    /// <param name="mergeCallback">The merge callback</param>
    internal TusCreationContext(
        string fileId,
        string fileIdParameterName,
        string? routeName,
        (string, object)[] routeValues,
        string? filename,
        string? directory,
        Func<UploadFileInfo, Task>? resourceCreatedCallback,
        Func<UploadFileInfo, Task>? uploadFinishedCallback,
        string? partialId,
        Func<IList<UploadFileInfo>, bool>? allowMergeCallback,
        Func<UploadFileInfo, IList<UploadFileInfo>, Task>? mergeCallback
    )
    {
        FileId = fileId;
        FileIdParameterName = fileIdParameterName;
        RouteValues = routeValues;
        Filename = filename;
        Directory = directory;
        ResourceCreatedCallback = resourceCreatedCallback;
        UploadFinishedCallback = uploadFinishedCallback;
        PartialId = partialId;
        AllowMergeCallback = allowMergeCallback;
        MergeCallback = mergeCallback;
    }

    /// <summary>
    /// Get the file id
    /// </summary>
    public string FileId { get; }

    /// <summary>
    /// Get the route name
    /// </summary>
    public string? RouteName { get; }

    /// <summary>
    /// Get the file id parameter name
    /// </summary>
    public string FileIdParameterName { get; }

    /// <summary>
    /// Get the route values
    /// </summary>
    public (string, object)[] RouteValues { get; }

    /// <summary>
    /// Get the filename
    /// </summary>
    public string? Filename { get; }

    /// <summary>
    /// Get the directory for the upload
    /// </summary>
    public string? Directory { get; }

    /// <summary>
    /// Get the resource created callback
    /// </summary>
    public Func<UploadFileInfo, Task>? ResourceCreatedCallback { get; }

    /// <summary>
    /// Get the upload finished callback
    /// </summary>
    public Func<UploadFileInfo, Task>? UploadFinishedCallback { get; }

    /// <summary>
    /// Get the partial id
    /// </summary>
    public string? PartialId { get; }

    /// <summary>
    /// Get the allow merge callback
    /// </summary>
    public Func<IList<UploadFileInfo>, bool>? AllowMergeCallback { get; }

    /// <summary>
    /// Get the merge callback
    /// </summary>
    public Func<UploadFileInfo, IList<UploadFileInfo>, Task>? MergeCallback { get; }

    /// <summary>
    /// Start either:
    /// <para>
    /// - Create single resource metadata, which could include upload data
    /// </para>
    /// <para>
    /// - Create partial resource metadata
    /// </para>
    /// <para>
    /// - Start merge partial resources into single file
    /// </para>
    /// </summary>
    /// <param name="context">The http context</param>
    /// <returns>An awaitable task</returns>
    public async ValueTask StartCreationAsync(HttpContext context)
    {
        if (context.RequestServices.GetService(typeof(ResourceCreationHandler)) is not ResourceCreationHandler resource
        || context.RequestServices.GetService<CreationFlow>() is not CreationFlow creationFlow)
        {
            throw new InvalidOperationException("Remember to register SolidTUS on program startup");
        }

        if (context.Items[TusResult.Name] is not TusResult tusResult)
        {
            throw new InvalidOperationException("Can only use this method in conjuction with either endpoint filter or action filter.");
        }

        resource.SetDetails(this, tusResult);
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

        var result = response.Map(creationFlow.PostResourceCreation);

        var (isSuccess, _, error) = result;
        if (!isSuccess)
        {
            context.Response.StatusCode = error.StatusCode;
            context.SetErrorHeaders(error);
        }
    }
};
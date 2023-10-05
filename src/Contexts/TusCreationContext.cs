using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SolidTUS.Models;

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
    /// <param name="routeTemplate">The route template</param>
    /// <param name="routeName">The route name</param>
    /// <param name="fileIdParameter">The file id route parameter</param>
    /// <param name="routeValues">The extra optional route values</param>
    /// <param name="filename">The optional filename</param>
    /// <param name="expirationStrategy">The expiration strategy</param>
    /// <param name="interval">The expiration interval</param>
    /// <param name="resourceCreatedCallback">The resource created callback</param>
    /// <param name="uploadFinishedCallback">The upload finished callback when using Creation-With-Upload</param>
    /// <param name="partialId">The partial id</param>
    /// <param name="allowMergeCallback">The allow merge callback</param>
    /// <param name="mergeCallback">The merge callback</param>
    internal TusCreationContext(
        string fileId,
        string routeTemplate,
        string routeName,
        (string, object) fileIdParameter,
        (string, object)[] routeValues,
        string? filename,
        ExpirationStrategy? expirationStrategy,
        TimeSpan? interval,

        Func<UploadFileInfo, Task>? resourceCreatedCallback,
        Func<UploadFileInfo, Task>? uploadFinishedCallback,

        string? partialId,

        Func<IList<UploadFileInfo>, bool>? allowMergeCallback,
        Func<UploadFileInfo, IList<UploadFileInfo>, Task>? mergeCallback
    )
    {
        FileId = fileId;
        RouteTemplate = routeTemplate;
        RouteName = routeName;
        FileIdParameter = fileIdParameter;
        RouteValues = routeValues;
        Filename = filename;
        ExpirationStrategy = expirationStrategy;
        Interval = interval;
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
    /// Get the route template
    /// </summary>
    public string RouteTemplate { get; }

    /// <summary>
    /// Get the route name
    /// </summary>
    public string RouteName { get; }

    /// <summary>
    /// Get the file id parameter
    /// </summary>
    public (string, object) FileIdParameter { get; }

    /// <summary>
    /// Get the route values
    /// </summary>
    public (string, object)[] RouteValues { get; }

    /// <summary>
    /// Get the filename
    /// </summary>
    public string? Filename { get; }

    /// <summary>
    /// Get the expiration strategy
    /// </summary>
    public ExpirationStrategy? ExpirationStrategy { get; }

    /// <summary>
    /// Get the interval for the expiration
    /// </summary>
    public TimeSpan? Interval { get; }

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

};
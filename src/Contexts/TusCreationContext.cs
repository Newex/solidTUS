using System;
using System.Threading.Tasks;
using SolidTUS.Constants;
using SolidTUS.Models;
using SolidTUS.Options;
using SolidTUS.Wrappers;

namespace SolidTUS.Contexts;

/// <summary>
/// Creation context for TUS
/// </summary>
public record class TusCreationContext
{
    private readonly ILinkGeneratorWrapper linkGenerator;

    /// <summary>
    /// Instantiate a new object of <see cref="TusCreationContext"/>
    /// </summary>
    /// <param name="linkGenerator">The link generator</param>
    public TusCreationContext(
        ILinkGeneratorWrapper linkGenerator
    )
    {
        this.linkGenerator = linkGenerator;
    }

    internal string? UploadUrl { get; set; }
    internal string? FileId { get; set; }
    internal ExpirationStrategy? ExpirationStrategy { get; set; }
    internal TimeSpan? Interval { get; set; }
    internal string? Filename { get; set; }
    internal Func<UploadFileInfo, Task>? UploadFinishedCallback { get; set; }
    internal Func<UploadFileInfo, Task>? ResourceCreatedCallback { get; set; }
    internal TusParallelContext? TusParallelContext { get; set; }

    /// <summary>
    /// Set the tus upload url endpoint
    /// </summary>
    /// <param name="url">The endpoint url</param>
    /// <returns>A tus creation context</returns>
    public TusCreationContext SetUploadUrl(string url)
    {
        UploadUrl = url;
        return this;
    }

    /// <summary>
    /// Set the tus upload url endpoint by route name and route values
    /// </summary>
    /// <remarks>
    /// If no route name is provided the default route name will be used.
    /// </remarks>
    /// <param name="routeValues">The route values</param>
    /// <param name="routeName">The route name</param>
    /// <returns>A tus creation context</returns>
    public TusCreationContext SetUploadUrl(object routeValues, string routeName = EndpointNames.UploadEndpoint)
    {
        UploadUrl = linkGenerator.GetPathByName(routeName, routeValues);
        return this;
    }

    /// <summary>
    /// Set the expiration strategy and the associated time span.
    /// </summary>
    /// <remarks>
    /// If null, the global options will be used from <see cref="TusOptions"/>
    /// </remarks>
    /// <param name="strategy">The expiration strategy</param>
    /// <param name="interval">The expiration interval</param>
    /// <returns>A tus creation context</returns>
    public TusCreationContext SetExpirationStrategy(ExpirationStrategy? strategy, TimeSpan? interval)
    {
        ExpirationStrategy = strategy;
        Interval = interval;
        return this;
    }

    /// <summary>
    /// Set the filename of the upload, as it will be stored on disk on the server.
    /// </summary>
    /// <remarks>
    /// There can be conflicts if the filename has the same name as anothe file on the server in the same directory.
    /// </remarks>
    /// <param name="filename">The unique filename</param>
    /// <returns>A tus creation context</returns>
    public TusCreationContext SetFilename(string filename)
    {
        Filename = filename;
        return this;
    }

    /// <summary>
    /// Set callback for when the tus request <c>Creation-With-Upload</c> has finished.
    /// </summary>
    /// <param name="callback">The callback</param>
    /// <returns>A tus creation context</returns>
    public TusCreationContext OnCreateWithUploadFinished(Func<UploadFileInfo, Task> callback)
    {
        UploadFinishedCallback = callback;
        return this;
    }

    /// <summary>
    /// Set the callback for when the tus request <c>Creation</c> has finished.
    /// </summary>
    /// <param name="callback">The callback</param>
    /// <returns>A tus creation context</returns>
    public TusCreationContext OnResourceCreated(Func<UploadFileInfo, Task> callback)
    {
        ResourceCreatedCallback = callback;
        return this;
    }

    /// <summary>
    /// Setup parallel uploads.
    /// <para>
    /// One (1) file will be split into parts from the client and send in pieces in parallel.
    /// Each piece will be assembled server side.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This is support for the tus <c>Concatenation</c> extension protocol.
    /// <para>
    /// Disclaimer: There might not be any appreciable speed up using this.
    /// </para>
    /// </remarks>
    /// <param name="uploadRouteTemplate">The template for the upload endpoint. Used to parse request urls to find the upload ids.</param>
    /// <returns>A tus parallel context</returns>
    public TusParallelContext WithParallelUploads(string uploadRouteTemplate)
    {
        var parallel = new TusParallelContext(uploadRouteTemplate, this);
        TusParallelContext = parallel;
        return parallel;
    }
}

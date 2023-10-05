using System;
using System.Threading.Tasks;

using SolidTUS.Constants;

using SolidTUS.Contexts;
using SolidTUS.Models;
using SolidTUS.Options;

namespace SolidTUS.Builders;

/// <summary>
/// Tus creation context builder
/// </summary>
public sealed class TusCreationContextBuilder
{
    private readonly string fileId;

    private ExpirationStrategy? ExpirationStrategy { get; set; }
    private TimeSpan? Interval { get; set; }
    private string? Filename { get; set; }
    private Func<UploadFileInfo, Task>? UploadFinishedCallback { get; set; }
    private Func<UploadFileInfo, Task>? ResourceCreatedCallback { get; set; }
    private TusParallelContextBuilder? TusParallelContext { get; set; }
    private string? RouteName { get; set; }
    private (string, string)[]? RouteValues { get; set; }

    internal TusCreationContextBuilder(
        string fileId
    )
    {
        this.fileId = fileId;
    }

    /// <summary>
    /// Set the route name to the upload endpoint
    /// </summary>
    /// <param name="routeName">The route name</param>
    /// <returns>A tus creation context</returns>
    public TusCreationContextBuilder SetRouteName(string routeName)
    {
        RouteName = routeName;
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
    public TusCreationContextBuilder SetExpirationStrategy(ExpirationStrategy? strategy, TimeSpan? interval)
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
    public TusCreationContextBuilder SetFilename(string filename)
    {
        Filename = filename;
        return this;
    }

    /// <summary>
    /// Set callback for when the tus request <c>Creation-With-Upload</c> has finished.
    /// </summary>
    /// <param name="callback">The callback</param>
    /// <returns>A tus creation context</returns>
    public TusCreationContextBuilder OnCreateWithUploadFinished(Func<UploadFileInfo, Task> callback)
    {
        UploadFinishedCallback = callback;
        return this;
    }

    /// <summary>
    /// Set the callback for when the tus request <c>Creation</c> has finished.
    /// </summary>
    /// <param name="callback">The callback</param>
    /// <returns>A tus creation context</returns>
    public TusCreationContextBuilder OnResourceCreated(Func<UploadFileInfo, Task> callback)
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
    /// <returns>A tus parallel context</returns>
    public TusParallelContextBuilder WithParallelUploads()
    {
        var parallel = new TusParallelContextBuilder(this);
        TusParallelContext = parallel;
        return parallel;
    }

    /// <summary>
    /// Construct the tus creation context used in the TUS request
    /// </summary>
    /// <param name="routeTemplate">The route template</param>
    /// <param name="routeValue">The route value for the file id</param>
    /// <param name="routeValues">The extra optional route values</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TusCreationContext Build(string routeTemplate, (string ParameterName, object ParameterValue) routeValue, params (string, object)[] routeValues)
    {
        return new TusCreationContext(
            fileId,
            routeTemplate,
            RouteName ?? EndpointNames.UploadEndpoint,
            routeValue,
            routeValues,
            Filename,
            ExpirationStrategy,
            Interval,
            ResourceCreatedCallback,
            UploadFinishedCallback,
            TusParallelContext?.PartialId,
            TusParallelContext?.AllowMergeCallback,
            TusParallelContext?.MergeCallback
        );
    }
}

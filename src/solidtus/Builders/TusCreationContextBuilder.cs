using System;
using System.Threading.Tasks;
using SolidTUS.Constants;
using SolidTUS.Contexts;
using SolidTUS.Models;

namespace SolidTUS.Builders;

/// <summary>
/// Tus creation context builder
/// </summary>
public sealed class TusCreationContextBuilder
{
    private readonly string fileId;

    private string? Filename { get; set; }
    private string? Directory { get; set; }
    private Func<UploadFileInfo, Task>? UploadFinishedCallback { get; set; }
    private Func<UploadFileInfo, Task>? ResourceCreatedCallback { get; set; }
    private TusParallelContextBuilder? TusParallelContext { get; set; }
    private string? RouteName { get; set; }

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
    /// Set the directory where the upload will be stored on disk, if using filesystem to store file.
    /// </summary>
    /// <param name="directory">The directory path</param>
    /// <returns>A tus creation context</returns>
    public TusCreationContextBuilder SetDirectory(string directory)
    {
        Directory = directory;
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
    /// <param name="fileIdParameterName">The name of the parameter for the file id</param>
    /// <param name="routeValues">The extra route values</param>
    /// <returns>A tus creation context</returns>
    public TusCreationContext Build(string fileIdParameterName, params (string, object)[] routeValues)
    {
        return new TusCreationContext(
            fileId,
            fileIdParameterName,
            RouteName,
            routeValues,
            Filename,
            Directory,
            ResourceCreatedCallback,
            UploadFinishedCallback,
            TusParallelContext?.PartialId,
            TusParallelContext?.AllowMergeCallback,
            TusParallelContext?.MergeCallback
        );
    }
}

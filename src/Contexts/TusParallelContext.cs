using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SolidTUS.Models;

namespace SolidTUS.Contexts;

/// <summary>
/// Parallel upload context for TUS
/// </summary>
public record TusParallelContext
{
    private readonly TusCreationContext creationContext;

    internal TusParallelContext(
        string routeTemplate,
        TusCreationContext creationContext
    )
    {
        RouteTemplate = routeTemplate;
        this.creationContext = creationContext;
    }

    internal string RouteTemplate { get; }
    internal string? PartialId { get; set; }
    internal Func<IList<UploadFileInfo>, bool> AllowMergeCallback { get; set; } = _ => true;
    internal Func<UploadFileInfo, IList<UploadFileInfo>, Task>? MergeCallback { get; set; }

    /// <summary>
    /// Allow or reject the merge of the files.
    /// <para>
    /// Parallel uploads needs to merge the partial files server side.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Default behaviour is to allow all merge requests.
    /// </remarks>
    /// <param name="allow">Callback that on true allows merge otherwise rejects the merge request</param>
    /// <returns>A parallel context</returns>
    public TusParallelContext AllowMerge(Func<IList<UploadFileInfo>, bool> allow)
    {
        AllowMergeCallback = allow;
        return this;
    }

    /// <summary>
    /// Callback for when the list of files has been merged.
    /// The first argument is the final merged upload, the second argument is the list of partial files in order.
    /// </summary>
    /// <param name="merged">Merged list of files in order</param>
    /// <returns>A parallel context</returns>
    public TusParallelContext OnMergedFiles(Func<UploadFileInfo, IList<UploadFileInfo>, Task> merged)
    {
        MergeCallback = merged;
        return this;
    }

    /// <summary>
    /// If the current request is a parallel
    /// </summary>
    /// <param name="partialId">The partial id</param>
    /// <returns>A parallel context</returns>
    public TusParallelContext SetPartialId(string partialId)
    {
        PartialId = partialId;
        return this;
    }

    /// <summary>
    /// Return to the tus creation context
    /// </summary>
    /// <returns>A tus creation context</returns>
    public TusCreationContext Done()
    {
        return creationContext;
    }
}

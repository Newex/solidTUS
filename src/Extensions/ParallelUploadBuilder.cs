using System;
using System.Collections.Generic;
using SolidTUS.Constants;
using SolidTUS.Contexts;
using SolidTUS.Models;

namespace SolidTUS.Extensions;

/// <summary>
/// Parallel upload builder
/// </summary>
/// <remarks>
/// Remember to provide this final instance to <see cref="TusCreationContext.ApplyParallelUploadsConfiguration(ParallelUploadConfig)"/>
/// </remarks>
public sealed class ParallelUploadBuilder
{
    private readonly string template;
    private readonly string routeName;

    private string? partialId;
    private string? partialIdName;
    private Func<IList<UploadFileInfo>, string>? finalId;
    private Func<IList<UploadFileInfo>, bool>? allow;
    private object? routeValues;

    /// <summary>
    /// Instantiate a new object of <see cref="ParallelUploadBuilder"/>
    /// </summary>
    /// <param name="template">The route template for the parallel uploads</param>
    /// <param name="routeName">The route name for the parallel uploads</param>
    internal ParallelUploadBuilder(string template, string routeName)
    {
        this.template = template;
        this.routeName = routeName;

    }

    /// <summary>
    /// Set the partial id of the parallel upload.
    /// </summary>
    /// <remarks>
    /// If not provided the original fileId will be used.
    /// </remarks>
    /// <param name="partialId">The partial id</param>
    /// <returns>A builder</returns>
    public ParallelUploadBuilder SetPartialId(string partialId)
    {
        this.partialId = partialId;
        return this;
    }

    /// <summary>
    /// Set the route values
    /// </summary>
    /// <param name="routeValues">The route values</param>
    /// <returns>A builder</returns>
    public ParallelUploadBuilder SetRouteValues(object routeValues)
    {
        this.routeValues = routeValues;
        return this;
    }

    /// <summary>
    /// Set the name for the partialId parameter. Default is <see cref="ParameterNames.ParallelPartialIdParameterName"/>
    /// </summary>
    /// <param name="partialIdName">The name of the variable as found in the template</param>
    /// <returns>A builder</returns>
    public ParallelUploadBuilder SetParallelIdParameterNameInTemplate(string partialIdName)
    {
        this.partialIdName = partialIdName;
        return this;
    }

    /// <summary>
    /// Set the handler for naming the final file when a collection of files should be merged.
    /// <para>If no handler is set, the fileId from the <see cref="TusCreationContext.StartCreationAsync(string, string?, string?, bool)"/> will be used.</para>
    /// </summary>
    /// <param name="setFinalId">The final file id handler</param>
    /// <returns>A builder</returns>
    public ParallelUploadBuilder OnFinalFileIdHandler(Func<IList<UploadFileInfo>, string> setFinalId)
    {
        finalId = setFinalId;
        return this;
    }

    /// <summary>
    /// The merge handler, if it returns true, the merge will be allowed otherwise the merge will be denied.
    /// </summary>
    /// <param name="allow">The merge allow callback</param>
    /// <returns>A builder</returns>
    public ParallelUploadBuilder OnMergeHandler(Func<IList<UploadFileInfo>, bool> allow)
    {
        this.allow = allow;
        return this;
    }

    /// <summary>
    /// Create the final parallel upload configuration.
    /// </summary>
    /// <remarks>
    /// Remember to apply the configuration to the context.
    /// </remarks>
    /// <returns>A parallel upload configuration object</returns>
    /// <exception cref="ArgumentNullException">Thrown if no handler for merge is provided</exception>
    public ParallelUploadConfig Build()
    {
        if (allow is null)
        {
            throw new ArgumentNullException(nameof(allow));
        }

        return new(template, partialIdName ?? ParameterNames.ParallelPartialIdParameterName, partialId, routeName, routeValues, allow, finalId);
    }

    /// <summary>
    /// The parallel upload configuration object
    /// </summary>
    public sealed record ParallelUploadConfig
    {
        internal ParallelUploadConfig(string template,
                                      string partialIdName,
                                      string? partialId,
                                      string routeName,
                                      object? routeValues,
                                      Func<IList<UploadFileInfo>, bool> allow,
                                      Func<IList<UploadFileInfo>, string>? setFinalId)
        {
            Template = template;
            PartialIdName = partialIdName;
            PartialId = partialId;
            RouteName = routeName;
            RouteValues = routeValues;

            Allow = allow;
            SetFinalId = setFinalId;
        }

        /// <summary>
        /// The route template to the parallel upload endpoint
        /// </summary>
        public string Template { get; }

        /// <summary>
        /// The name of the variable in the parallel upload.
        /// </summary>
        /// <remarks>
        /// Default name is: <see cref="ParameterNames.ParallelPartialIdParameterName"/>
        /// </remarks>
        public string PartialIdName { get; }

        /// <summary>
        /// The partial id of the parallel upload
        /// </summary>
        public string? PartialId { get; }


        /// <summary>
        /// The name of the route to the parallel upload endpoint
        /// </summary>
        /// <remarks>
        /// The default name is: <see cref="EndpointNames.ParallelEndpoint"/>
        /// </remarks>
        public string RouteName { get; }

        /// <summary>
        /// The route values
        /// </summary>
        public object? RouteValues { get; }


        /// <summary>
        /// The merge allow callback function. If returning true the merge will be accepted otherwise denied.
        /// </summary>
        public Func<IList<UploadFileInfo>, bool> Allow { get; }

        /// <summary>
        /// The optional renaming of the final file id for the collection of partial uploads.
        /// </summary>
        public Func<IList<UploadFileInfo>, string>? SetFinalId { get; }
    }
}

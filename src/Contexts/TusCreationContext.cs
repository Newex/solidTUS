using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SolidTUS.Constants;
using SolidTUS.Extensions;
using SolidTUS.Handlers;
using SolidTUS.Models;
using SolidTUS.Options;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;
using SolidTUS.Wrappers;

using static SolidTUS.Extensions.ParallelUploadBuilder;

namespace SolidTUS.Contexts;

/// <summary>
/// The TUS creation context
/// </summary>
public class TusCreationContext
{
    private readonly long? maxSize;
    private readonly bool withUpload;
    private readonly PartialMode partialMode;
    private readonly IList<string> partialUrls;
    private readonly PipeReader reader;
    private readonly IUploadStorageHandler uploadStorageHandler;
    private readonly IUploadMetaHandler uploadMetaHandler;
    private readonly CancellationToken cancellationToken;
    private readonly ILogger logger;
    private readonly ILinkGeneratorWrapper linkGenerator;
    private readonly Action<string> onCreated;
    private readonly Action<long> onUpload;
    private RouteNameValuePair? uploadRoute = null;
    private bool isCalledMoreThanOnce = false;
    private ParallelUploadConfig? parallelUploadConfig;

    private Func<UploadFileInfo, Task>? onResourceCreatedAsync;
    private Func<Task>? onUploadFinishedAsync;

    /// <summary>
    /// Instantiate a new object of <see cref="TusCreationContext"/>
    /// </summary>
    /// <param name="withUpload">True if request includes upload otherwise false</param>
    /// <param name="partialMode">The upload request is either single upload, partial or final</param>
    /// <param name="partialUrls">The partial urls</param>
    /// <param name="uploadFileInfo">The upload file info</param>
    /// <param name="onCreated">Callback when resource has been created</param>
    /// <param name="onUpload">Callback when resource has been uploaded</param>
    /// <param name="reader">The upload reader</param>
    /// <param name="uploadStorageHandler">The upload storage handler</param>
    /// <param name="uploadMetaHandler">The upload meta handler</param>
    /// <param name="linkGenerator">The link generator</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <param name="options">The tus options</param>
    /// <param name="logger">The optional logger</param>
    public TusCreationContext(
        bool withUpload,
        PartialMode partialMode,
        IList<string> partialUrls,
        UploadFileInfo uploadFileInfo,
        Action<string> onCreated,
        Action<long> onUpload,
        PipeReader reader,
        IUploadStorageHandler uploadStorageHandler,
        IUploadMetaHandler uploadMetaHandler,
        ILinkGeneratorWrapper linkGenerator,
        CancellationToken cancellationToken,
        IOptions<TusOptions> options,
        ILogger? logger = null
    )
    {
        this.withUpload = withUpload;
        this.partialMode = partialMode;
        this.partialUrls = partialUrls;
        UploadFileInfo = uploadFileInfo;
        this.onCreated = onCreated;
        this.onUpload = onUpload;
        this.reader = reader;
        this.uploadStorageHandler = uploadStorageHandler;
        this.uploadMetaHandler = uploadMetaHandler;
        this.cancellationToken = cancellationToken;
        this.linkGenerator = linkGenerator;
        maxSize = options.Value.MaxSize;
        this.logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Get the upload file info
    /// </summary>
    public UploadFileInfo UploadFileInfo { get; init; }

    /// <summary>
    /// Function called when resource has been created
    /// </summary>
    /// <param name="callback">The callback function</param>
    public void OnResourceCreated(Func<UploadFileInfo, Task> callback)
    {
        onResourceCreatedAsync = callback;
    }

    /// <summary>
    /// Function called when an optional creation contains upload data that has finished uploading data
    /// </summary>
    /// <remarks>
    /// Is not called if the upload is only a partial upload
    /// </remarks>
    /// <param name="callback"></param>
    public void OnUploadFinished(Func<Task> callback)
    {
        onUploadFinishedAsync = callback;
    }

    /// <summary>
    /// Set the individual upload expiration strategy
    /// </summary>
    /// <remarks>
    /// Must be called prior to starting the resource creation.
    /// </remarks>
    /// <param name="expiration">The expiration strategy</param>
    /// <param name="interval">The optional time span interval</param>
    public void SetExpirationStrategy(ExpirationStrategy expiration, TimeSpan? interval = null)
    {
        // Question should this be set for partial uploads?
        UploadFileInfo.ExpirationStrategy = expiration;
        UploadFileInfo.Interval = interval;
    }

    /// <summary>
    /// Set the route values to the <c>TusUpload</c> endpoint
    /// </summary>
    /// <typeparam name="T">The route value type, must be a reference type</typeparam>
    /// <param name="routeValues">The route values</param>
    /// <param name="routeName">The route name. If null the default name will be used <see cref="EndpointNames.UploadEndpoint"/></param>
    public void SetUploadRouteValues<T>(T routeValues, string? routeName = null)
    where T : class
    {
        uploadRoute = new(routeName, routeValues);
    }

    /// <summary>
    /// Setup parallel upload configuration
    /// </summary>
    /// <param name="templateForParallelUpload">The route template for the parallel upload endpoint</param>
    /// <param name="routeNameForParallelUpload">The route name for the parallel upload endpoint. If null; the default value will be used <see cref="EndpointNames.ParallelEndpoint"/></param>
    /// <returns>A parallel upload configuration builder</returns>
    public ParallelUploadBuilder SetupParallelUploads(string templateForParallelUpload, string? routeNameForParallelUpload = null)
    {
        return new ParallelUploadBuilder(templateForParallelUpload, routeNameForParallelUpload ?? EndpointNames.ParallelEndpoint);
    }

    /// <summary>
    /// Apply the configurations from the parallel upload setup
    /// </summary>
    /// <param name="config">The configuration settings</param>
    public void ApplyParallelUploadsConfiguration(ParallelUploadConfig config)
    {
        parallelUploadConfig = config;
    }

    /// <summary>
    /// Start resource creation
    /// </summary>
    /// <remarks>
    /// The metadata upload file info will only be deleted if the request contains the whole upload. Otherwise the client will be directed to the upload location.
    /// It is recommended to create a unique and different filename to avoid any malicious uploads overwriting other files.
    /// The metadata.json file that is created by default by <see cref="FileUploadMetaHandler"/> creates a filename using the file id.
    /// Be careful not to overwrite other uploads that are in progress by naming them the same.
    /// </remarks>
    /// <param name="fileId">The file Id</param>
    /// <param name="filename">The filename on disk. Defaults to <paramref name="fileId"/> value</param>
    /// <param name="deleteInfoOnDone">True if the metadata upload info file should be deleted when upload has been finished otherwise false</param>
    /// <returns>An awaitable task</returns>
    public async Task StartCreationAsync(string fileId, string? filename = null, bool deleteInfoOnDone = false)
    {
        // Three possible directions
        // 1 - upload single file (normal)
        // 2 - upload part parallel
        // 3 - merge parts

        if (partialMode == PartialMode.None)
        {
            var routeName = uploadRoute.HasValue ? (uploadRoute.Value.RouteName ?? EndpointNames.UploadEndpoint) : EndpointNames.UploadEndpoint;
            var routeValues = uploadRoute.HasValue ? uploadRoute.Value.RouteValues : null;
            var uploadUrl = linkGenerator.GetPathByName(routeName, routeValues);
            if (uploadUrl is null)
            {
                throw new ArgumentException("Could not create URL to the upload endpoint route");
            }

            UploadFileInfo.FileId = fileId;
            await UploadBeginAsync(fileId, uploadUrl, filename, deleteInfoOnDone);
        }
        else if (partialMode == PartialMode.Partial)
        {
            // Start new parallel upload
            if (parallelUploadConfig is null)
            {
                throw new ArgumentNullException("Must provide parallel upload configuration");
            }

            var routeName = parallelUploadConfig.RouteName;
            var routeValues = parallelUploadConfig.RouteValues;
            var uploadUrl = linkGenerator.GetPathByName(routeName, routeValues);
            if (uploadUrl is null)
            {
                throw new ArgumentException("Could not create URL to the parallel upload endpoint route");
            }

            UploadFileInfo.PartialId = parallelUploadConfig.PartialId ?? fileId;
            await UploadBeginAsync(UploadFileInfo.PartialId, uploadUrl, filename, deleteInfoOnDone);
        }
        else if (partialMode == PartialMode.Final)
        {
            // Begin merging final uploads
            if (parallelUploadConfig is null)
            {
                throw new ArgumentNullException("Must provide parallel upload configuration");
            }

            var partialIdStrings = partialUrls.Select(url => ConcatenationRequestHandler.GetTemplateValue(url, parallelUploadConfig.Template, parallelUploadConfig.PartialIdName));
            var partials = new List<UploadFileInfo>();
            var currentSize = 0L;
            foreach (var partialId in partialIdStrings)
            {
                if (partialId is null)
                {
                    logger.LogError("Cannot extract partial id from the given template: {Template} and the provided route for the parameter named: {ParameterName}", parallelUploadConfig.Template, parallelUploadConfig.PartialIdName);
                    throw new ArgumentException("Partial id not extractet check the template and/or parameter name for the parallel upload");
                }

                var info = await uploadMetaHandler.GetPartialResourceAsync(partialId, cancellationToken);
                if (info is null)
                {
                    logger.LogError("Missing partial upload info for {PartialId}", partialId);
                    throw new InvalidOperationException("Missing partial upload file info");
                }

                var duplicate = partials.Any(x => x.PartialId == info.PartialId);
                if (duplicate)
                {
                    logger.LogError("Partial files must not contain duplicates {PartialId}", partialId);
                    throw new ArgumentException("Partial id duplicated, must only use 1 partial upload once per upload");
                }

                partials.Add(info);
                currentSize += info.ByteOffset;
            }

            if (maxSize.HasValue && currentSize > maxSize.Value)
            {
                throw new InvalidOperationException("The maximum upload size is exceeded");
            }

            var allowed = parallelUploadConfig.Allow(partials);
            if (!allowed)
            {
                throw new InvalidOperationException("Not allowed to merge files");
            }

            UploadFileInfo.OnDiskFilename = filename ?? fileId;
            UploadFileInfo.FileId = fileId;
            UploadFileInfo.ByteOffset = currentSize;
            var finalInfo = UploadFileInfo with
            {
                FileSize = currentSize
            };
            var merged = await uploadStorageHandler.MergePartialFilesAsync(finalInfo, partials, cancellationToken);
            if (!merged.Done)
            {
                throw new InvalidOperationException("Merged file does not equal partial uploads");
            }

            merged.ConcatHeaderFinal = "final;" + string.Join(" ", partialUrls);
            merged.IsPartial = false;
            var createInfo = await uploadMetaHandler.CreateResourceAsync(merged, cancellationToken);

            var routeName = uploadRoute.HasValue ? (uploadRoute.Value.RouteName ?? EndpointNames.UploadEndpoint) : EndpointNames.UploadEndpoint;
            var routeValues = uploadRoute.HasValue ? uploadRoute.Value.RouteValues : null;
            var uploadUrl = linkGenerator.GetPathByName(routeName, routeValues);
            if (uploadUrl is null)
            {
                throw new ArgumentException("Could not create URL to the upload endpoint route");
            }

            onCreated(uploadUrl);
        }
    }

    private async Task UploadBeginAsync(string fileId, string uploadLocationUrl, string? filename = null, bool deleteInfoOnDone = false)
    {
        if (isCalledMoreThanOnce)
        {
            return;
        }

        UploadFileInfo.OnDiskFilename = filename ?? fileId;

        var created = await uploadMetaHandler.CreateResourceAsync(UploadFileInfo, cancellationToken);
        if (created)
        {
            // Server side callback
            onCreated(uploadLocationUrl);

            if (onResourceCreatedAsync is not null)
            {
                // Client side callback
                await onResourceCreatedAsync(UploadFileInfo);
            }
        }

        if (withUpload)
        {
            // Can append if we dont need to worry about checksum
            var written = await uploadStorageHandler.OnPartialUploadAsync(reader, UploadFileInfo, null, cancellationToken);

            // First server callback -->
            onUpload(written);

            // Finished upload -->
            var isFinished = written == UploadFileInfo.FileSize;
            if (isFinished && onUploadFinishedAsync is not null)
            {
                // Client callback -->
                await onUploadFinishedAsync();
            }

            if (isFinished && deleteInfoOnDone)
            {
                await uploadMetaHandler.DeleteUploadFileInfoAsync(UploadFileInfo, cancellationToken);
            }
        }

        isCalledMoreThanOnce = true;
    }
}

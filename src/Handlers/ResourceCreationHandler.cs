using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SolidTUS.Constants;
using SolidTUS.Contexts;
using SolidTUS.Extensions;
using SolidTUS.Models;
using SolidTUS.Options;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;
using SolidTUS.Wrappers;

namespace SolidTUS.Handlers;

/// <summary>
/// Resource creation handler
/// </summary>
public class ResourceCreationHandler
{
    private IHeaderDictionary? responseHeaders;
    private TusCreationContext? userOptions;
    private RequestContext? requestContext;
    private PipeReader? reader;

    private readonly TusOptions globalOptions;
    private readonly IUploadMetaHandler uploadMetaHandler;
    private readonly IUploadStorageHandler uploadStorageHandler;
    private readonly ISystemClock clock;
    private readonly ILinkGeneratorWrapper linkGenerator;

    private readonly ILogger<ResourceCreationHandler> logger;

    /// <summary>
    /// Instantiate a new object of <see cref="ResourceCreationHandler"/>
    /// </summary>
    /// <param name="clock">The clock provider</param>
    /// <param name="linkGenerator">The link generator wrapper</param>
    /// <param name="uploadMetaHandler">The upload meta handler</param>
    /// <param name="uploadStorageHandler">The upload storage handler</param>
    /// <param name="options">The TUS options</param>
    /// <param name="logger">The optional logger</param>
    public ResourceCreationHandler(
        ISystemClock clock,
        ILinkGeneratorWrapper linkGenerator,
        IUploadMetaHandler uploadMetaHandler,
        IUploadStorageHandler uploadStorageHandler,
        IOptions<TusOptions> options,
        ILogger<ResourceCreationHandler>? logger = null
    )
    {
        this.clock = clock;
        this.linkGenerator = linkGenerator;

        globalOptions = options.Value;
        this.uploadMetaHandler = uploadMetaHandler;
        this.uploadStorageHandler = uploadStorageHandler;
        this.logger = logger ?? NullLogger<ResourceCreationHandler>.Instance;
    }

    /// <summary>
    /// Set required details from the user and the request
    /// </summary>
    /// <param name="tusContext">The user tus options</param>
    /// <param name="requestContext">The request context</param>
    public void SetDetails(TusCreationContext tusContext, RequestContext requestContext)
    {
        userOptions = tusContext;
        this.requestContext = requestContext;
    }

    /// <summary>
    /// Set the body stream
    /// </summary>
    /// <param name="reader">The pipe reader for the body stream</param>
    public void SetPipeReader(PipeReader reader)
    {
        this.reader = reader;
    }

    /// <summary>
    /// Set the response headers
    /// </summary>
    /// <param name="responseHeaders">The response headers</param>
    public void SetResponseHeaders(IHeaderDictionary responseHeaders)
    {
        this.responseHeaders = responseHeaders;
    }

    /// <summary>
    /// Create resource of either a partial file or a whole file
    /// </summary>
    /// <param name="hasUpload">True if request contains upload data otherwise false</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A response context or an error</returns>
    public async Task<Result<ResponseContext>> CreateResourceAsync(bool hasUpload, CancellationToken cancellationToken)
    {
        if (userOptions is null
            || requestContext is null
            || responseHeaders is null)
        {
            throw new UnreachableException();
        }
        var response = new ResponseContext(responseHeaders);

        if (userOptions.FileId is null)
        {
            logger.LogError("Must provide file id for the resource");
            return HttpError.InternalServerError().Response();
        }

        var uploadUrl = linkGenerator.GetPathByName(userOptions.RouteName, userOptions.FileIdParameter, userOptions.RouteValues);
        if (uploadUrl is null)
        {
            logger.LogError("Must have an upload endpoint to upload the resource");
            return HttpError.InternalServerError().Response();
        }
        response.LocationUrl = uploadUrl;

        var now = clock.UtcNow;
        var isPartial = requestContext.PartialMode == PartialMode.Partial;
        var strategy = userOptions.ExpirationStrategy ?? globalOptions.ExpirationStrategy;
        var interval = userOptions.Interval
            ?? (strategy == ExpirationStrategy.AbsoluteExpiration
                ? globalOptions.AbsoluteInterval
                : globalOptions.SlidingInterval);
        var uploadInfo = new UploadFileInfo
        {
            FileId = userOptions.FileId,
            CreatedDate = now,
            ByteOffset = 0L,
            FileSize = requestContext.FileSize,
            Metadata = requestContext.Metadata,
            RawMetadata = requestContext.RawMetadata,
            ExpirationStrategy = strategy,
            Interval = interval,
            IsPartial = isPartial,
            ConcatHeaderFinal = null,
            ExpirationDate = ExpirationRequestHandler.CalculateExpiration(
                strategy,
                now,
                now,
                null,
                userOptions.Interval ?? globalOptions.AbsoluteInterval,
                userOptions.Interval ?? globalOptions.SlidingInterval),
            LastUpdatedDate = null,
            PartialId = isPartial ? (userOptions.PartialId ?? userOptions.FileId) : null,
            OnDiskFilename = userOptions.Filename ?? userOptions.FileId,
        };

        bool create = await uploadMetaHandler.CreateResourceAsync(uploadInfo, cancellationToken);
        if (create)
        {
            response.UploadFileInfo = uploadInfo;
            if (!hasUpload)
            {
                if (userOptions.ResourceCreatedCallback is not null)
                {
                    await userOptions.ResourceCreatedCallback(uploadInfo);
                }
                logger.LogInformation("Created resource {@UploadFileInfo}", uploadInfo);
                return response.Wrap();
            }


            if (reader is null)
            {
                throw new InvalidOperationException("Missing body stream to retrieve upload in Creation-With-Upload");
            }

            await uploadStorageHandler.OnPartialUploadAsync(reader, uploadInfo, requestContext.ChecksumContext, cancellationToken);
            if (uploadInfo.Done)
            {
                if (userOptions.ResourceCreatedCallback is not null)
                {
                    await userOptions.ResourceCreatedCallback(uploadInfo);
                }

                if (userOptions.UploadFinishedCallback is not null)
                {
                    await userOptions.UploadFinishedCallback(uploadInfo);
                }

                logger.LogInformation("Created resource with upload data {@UploadFileInfo}", uploadInfo);
                return response.Wrap();
            }
        }

        return HttpError.InternalServerError().Response();
    }

    /// <summary>
    /// Merge files if allowed
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A response context or an error</returns>
    public async Task<Result<ResponseContext>> MergeFilesAsync(CancellationToken cancellationToken)
    {
        if (responseHeaders is null || userOptions is null)
        {
            throw new UnreachableException();
        }
        var result = new ResponseContext(responseHeaders);

        if (requestContext is null || requestContext.Urls is null)
        {
            throw new UnreachableException();
        }

        var infos = new List<UploadFileInfo>();
        foreach (var url in requestContext.Urls)
        {
            var partialId = ConcatenationRequestHandler.GetTemplateValue(url, userOptions.RouteTemplate, userOptions.FileIdParameter.Item1);
            if (partialId is null)
            {
                return HttpError.NotFound("Partial resource not found").Response();
            }

            var info = await uploadMetaHandler.GetPartialResourceAsync(partialId, cancellationToken);
            if (info is null)
            {
                return HttpError.InternalServerError().Response();
            }

            if (!info.Done)
            {
                return HttpError.BadRequest("Cannot merge partial files that have not finished uploading").Response();
            }

            infos.Add(info);
        }

        var allowed = true;
        if (userOptions.AllowMergeCallback is not null)
        {
            allowed = userOptions.AllowMergeCallback(infos);
        }

        if (!allowed)
        {
            logger.LogInformation("Denied merge of {@Files}", infos);
            return HttpError.Forbidden().Response();
        }

        var now = clock.UtcNow;
        var finalInfo = new UploadFileInfo
        {
            FileId = userOptions.FileId,
            ByteOffset = 0L,
            ConcatHeaderFinal = requestContext.RequestHeaders[TusHeaderNames.UploadConcat],
            CreatedDate = now,
            ExpirationDate = null, // Uploaded file should not expire
            ExpirationStrategy = ExpirationStrategy.Never,
            FileSize = infos.Aggregate(0L, (size, curr) => curr.FileSize.GetValueOrDefault() + size),
            Interval = null,
            IsPartial = false, // This is a complete merged file
            LastUpdatedDate = null,
            Metadata = requestContext.Metadata,
            OnDiskFilename = userOptions.Filename ?? userOptions.FileId,
            PartialId = null,
            RawMetadata = requestContext.RawMetadata
        };

        var merged = await uploadStorageHandler.MergePartialFilesAsync(finalInfo, infos, cancellationToken);
        if (userOptions.MergeCallback is not null)
        {
            await userOptions.MergeCallback(finalInfo, infos);
        }

        result.UploadFileInfo = merged;

        // Create url to new file
        result.LocationUrl = linkGenerator.GetPathToUploadWithWhenKey(userOptions.FileIdParameter.Item1, userOptions.FileIdParameter.Item2, userOptions.RouteName);
        return result.Wrap();
    }
}

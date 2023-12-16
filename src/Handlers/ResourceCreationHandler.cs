using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SolidTUS.Constants;
using SolidTUS.Contexts;
using SolidTUS.Models;
using SolidTUS.Options;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;
using SolidTUS.Wrappers;

namespace SolidTUS.Handlers;

/// <summary>
/// Resource creation handler
/// </summary>
internal class ResourceCreationHandler
{
    private TusCreationContext? userOptions;
    private TusResult? tusResult;
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
    /// <param name="tusResult">The request context</param>
    public void SetDetails(TusCreationContext tusContext, TusResult tusResult)
    {
        userOptions = tusContext;
        this.tusResult = tusResult;
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
    /// Create resource of either a partial file or a whole file
    /// </summary>
    /// <param name="hasUpload">True if request contains upload data otherwise false</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A response context or an error</returns>
    public async Task<Result<TusResult, HttpError>> CreateResourceAsync(bool hasUpload, CancellationToken cancellationToken)
    {
        if (userOptions is null
            || tusResult is null
           )
        {
            throw new UnreachableException();
        }

        if (userOptions.FileId is null)
        {
            logger.LogError("Must provide file id for the resource");
            return HttpError.InternalServerError();
        }

        var isPartial = tusResult.PartialMode == PartialMode.Partial;
        var fileId = isPartial ? (userOptions.PartialId ?? userOptions.FileId) : userOptions.FileId;
        var uploadUrl = linkGenerator.GetPathByName(userOptions.RouteName ?? tusResult.UploadRouteName!, (userOptions.FileIdParameterName, fileId), userOptions.RouteValues);
        if (uploadUrl is null)
        {
            logger.LogError("Must have an upload endpoint to upload the resource");
            return HttpError.InternalServerError();
        }
        tusResult.LocationUrl = uploadUrl;

        var now = clock.UtcNow;
        var uploadInfo = new UploadFileInfo
        {
            FileId = fileId,
            CreatedDate = now,
            FileSize = tusResult.FileSize,
            Metadata = tusResult.Metadata,
            RawMetadata = tusResult.RawMetadata,
            IsPartial = isPartial,
            ConcatHeaderFinal = null,
            ExpirationDate = ExpirationRequestHandler.CalculateExpiration(
                globalOptions.ExpirationStrategy,
                now,
                now,
                null,
                globalOptions.AbsoluteInterval,
                globalOptions.SlidingInterval),
            LastUpdatedDate = null,
            OnDiskFilename = userOptions.Filename ?? fileId,
            OnDiskDirectoryPath = userOptions.Directory
        };

        bool create = await uploadMetaHandler.CreateResourceAsync(uploadInfo, cancellationToken);
        if (create)
        {
            tusResult.UploadFileInfo = uploadInfo;
            if (!hasUpload)
            {
                if (userOptions.ResourceCreatedCallback is not null)
                {
                    await userOptions.ResourceCreatedCallback(uploadInfo);
                }
                logger.LogInformation("Created resource {@UploadFileInfo}", uploadInfo);
                return tusResult;
            }


            if (reader is null)
            {
                throw new InvalidOperationException("Missing body stream to retrieve upload in Creation-With-Upload");
            }

            await uploadStorageHandler.OnPartialUploadAsync(reader, uploadInfo, tusResult.ChecksumContext, cancellationToken);
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
                return tusResult;
            }
        }

        logger.LogError("Could not create upload resource {@UploadInfo}", uploadInfo);
        return HttpError.InternalServerError();
    }

    /// <summary>
    /// Merge files if allowed
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A response context or an error</returns>
    public async Task<Result<TusResult, HttpError>> MergeFilesAsync(CancellationToken cancellationToken)
    {
        if (userOptions is null)
        {
            throw new UnreachableException();
        }

        if (tusResult is null || tusResult.Urls is null)
        {
            throw new UnreachableException();
        }

        var infos = new List<UploadFileInfo>();
        foreach (var url in tusResult.Urls)
        {
            var partialId = ConcatenationRequestHandler.GetTemplateValue(url, tusResult.UploadRouteTemplate!, userOptions.FileIdParameterName);
            if (partialId is null)
            {
                logger.LogError("Could not find partial resource with url {PartialUrl} for merging", url);
                return HttpError.NotFound("Partial resource not found");
            }

            var info = await uploadMetaHandler.GetResourceAsync(partialId, cancellationToken);
            if (info is null)
            {
                logger.LogError("Could not find partial resource with id {PartialId} for merging", partialId);
                return HttpError.InternalServerError();
            }

            if (!info.Done)
            {
                logger.LogError("Partial resource {PartialId} has not yet finished uploading, cannot merge unfinished uploads", partialId);
                return HttpError.BadRequest("Cannot merge partial files that have not finished uploading");
            }

            if (!info.IsPartial)
            {
                logger.LogError("Cannot merge non-partial files {@File}", info);
                return HttpError.BadRequest("Cannot merge non-partial files");
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
            return HttpError.Forbidden();
        }

        var now = clock.UtcNow;
        var finalInfo = new UploadFileInfo
        {
            FileId = userOptions.FileId,
            ConcatHeaderFinal = tusResult.RequestHeaders[TusHeaderNames.UploadConcat],
            CreatedDate = now,
            ExpirationDate = null, // Uploaded file should not expire
            FileSize = infos.Aggregate(0L, (size, curr) => curr.FileSize.GetValueOrDefault() + size),
            IsPartial = false, // This is a complete merged file
            LastUpdatedDate = null,
            Metadata = tusResult.Metadata,
            OnDiskFilename = userOptions.Filename ?? userOptions.FileId,
            RawMetadata = tusResult.RawMetadata
        };

        var merged = await uploadStorageHandler.MergePartialFilesAsync(finalInfo, infos, cancellationToken);
        if (userOptions.MergeCallback is not null)
        {
            await userOptions.MergeCallback(finalInfo, infos);
        }

        if (merged is null)
        {
            logger.LogError("Error occurred could not merge {@PartialFiles}", infos);
            return HttpError.InternalServerError();
        }

        logger.LogInformation("Merged files into {@FinalInfo} with the {@PartialInfos}", merged, infos);
        tusResult.UploadFileInfo = merged;

        // Create url to new file
        tusResult.LocationUrl = linkGenerator.GetPathToUploadWithWhenKey(userOptions.FileIdParameterName, userOptions.FileId, userOptions.RouteName ?? tusResult.UploadRouteName!);

        // Create final info
        await uploadMetaHandler.CreateResourceAsync(finalInfo, CancellationToken.None);

        if (globalOptions.DeletePartialFilesOnMerge)
        {
            foreach (var info in infos)
            {
                await uploadStorageHandler.DeleteFileAsync(info, CancellationToken.None);
                await uploadMetaHandler.DeleteUploadFileInfoAsync(info, CancellationToken.None);
            }
        }

        return tusResult;
    }
}

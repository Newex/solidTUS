using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using SolidTUS.Constants;
using SolidTUS.Contexts;
using SolidTUS.Extensions;
using SolidTUS.Handlers;
using SolidTUS.Models;
using SolidTUS.ProtocolHandlers;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;

namespace SolidTUS.ProtocolFlows;

/// <summary>
/// The upload flow
/// </summary>
public class UploadFlow
{
    private readonly CommonRequestHandler common;
    private readonly PatchRequestHandler patch;
    private readonly ChecksumRequestHandler checksumHandler;
    private readonly ExpirationRequestHandler expirationRequestHandler;
    private readonly IUploadStorageHandler uploadStorageHandler;
    private readonly IUploadMetaHandler uploadMetaHandler;

    /// <summary>
    /// Instantiate a new object of <see cref="UploadFlow"/>
    /// </summary>
    /// <param name="commonRequestHandler">The common request handler</param>
    /// <param name="patchRequestHandler">The patch request handler</param>
    /// <param name="checksumRequestHandler">The checksum request handler</param>
    /// <param name="expirationRequestHandler">The expiration request handler</param>
    /// <param name="uploadStorageHandler">The upload storage handler</param>
    /// <param name="uploadMetaHandler">The upload meta handler</param>
    public UploadFlow(
        CommonRequestHandler commonRequestHandler,
        PatchRequestHandler patchRequestHandler,
        ChecksumRequestHandler checksumRequestHandler,
        ExpirationRequestHandler expirationRequestHandler,
        IUploadStorageHandler uploadStorageHandler,
        IUploadMetaHandler uploadMetaHandler
    )
    {
        common = commonRequestHandler;
        patch = patchRequestHandler;
        checksumHandler = checksumRequestHandler;
        this.expirationRequestHandler = expirationRequestHandler;
        this.uploadStorageHandler = uploadStorageHandler;
        this.uploadMetaHandler = uploadMetaHandler;
    }

    /// <summary>
    /// A HEAD request determines the current status of an upload
    /// </summary>
    /// <param name="context">The request context</param>
    /// <param name="fileId">The file Id</param>
    /// <returns>Either an error or a request context</returns>
    public async ValueTask<Result<RequestContext>> GetUploadStatusAsync(RequestContext context, string fileId)
    {
        context.FileID = fileId;
        var noStoreCache = HeadRequestHandler.SetResponseCacheControl(context);
        var uploadInfoExists = await common.CheckUploadFileInfoExistsAsync(noStoreCache);
        var uploadOffset = uploadInfoExists.Map(c => HeadRequestHandler.SetUploadOffsetHeader(c));
        var setFileSizeResponseHeaders = uploadOffset.Map(c => HeadRequestHandler.SetUploadLengthOrDeferred(c));

        // Set metadata headers
        var result = setFileSizeResponseHeaders.Map(c => HeadRequestHandler.SetMetadataHeader(c));
        return result;
    }

    /// <summary>
    /// A PATCH request continues or starts an upload
    /// </summary>
    /// <param name="context">The request context</param>
    /// <param name="fileId">The file ID</param>
    /// <returns>Either an error or a request context</returns>
    public async ValueTask<Result<RequestContext>> StartUploadingAsync(RequestContext context, string fileId)
    {
        context.FileID = fileId;
        var contentType = PatchRequestHandler.CheckContentType(context);
        var uploadOffset = contentType.Bind(PatchRequestHandler.CheckUploadOffset);
        var uploadInfoExists = await uploadOffset.BindAsync(async c => await common.CheckUploadFileInfoExistsAsync(c));
        var byteOffset = uploadInfoExists.Bind(PatchRequestHandler.CheckConsistentByteOffset);
        var uploadLength = byteOffset.Bind(patch.CheckUploadLength);
        var uploadSize = uploadLength.Bind(PatchRequestHandler.CheckUploadExceedsFileSize);
        var uploadExpired = await uploadSize.BindAsync(expirationRequestHandler.CheckExpirationAsync);
        var uploadUpdatedDate = uploadExpired.Map(common.SetUpdatedDate);

        var setExpiration = uploadUpdatedDate.Map(expirationRequestHandler.SetExpiration);
        return setExpiration;
    }

    /// <summary>
    /// The checksum flow
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an http error or a request context</returns>
    public Result<RequestContext> ChecksumFlow(RequestContext context)
    {
        var hasChecksum = context.RequestHeaders.ContainsKey(TusHeaderNames.UploadChecksum);
        if (!hasChecksum)
        {
            return context.Wrap();
        }

        // Check checksum
        var parse = ChecksumRequestHandler.ParseChecksum(context);
        return checksumHandler.SetChecksum(context, parse);
    }

    /// <summary>
    /// Create upload context from the request context
    /// </summary>
    /// <param name="context">The request context</param>
    /// <param name="reader">The pipe reader</param>
    /// <param name="onDone">The callback function</param>
    /// <param name="onError">The callback function when an error occurs</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>An upload context or null</returns>
    public TusUploadContext? CreateUploadContext(Result<RequestContext> context, PipeReader reader, Action<UploadFileInfo> onDone, Action<HttpError> onError, CancellationToken cancellationToken)
    {
        var requestContext = context.Match(c => c, _ => null!);
        if (requestContext is null)
        {
            return null;
        }

        var uploadFileInfo = requestContext.UploadFileInfo;
        return new TusUploadContext(
            requestContext.ChecksumContext,
            uploadMetaHandler,
            uploadStorageHandler,
            reader,
            onDone,
            onError,
            uploadFileInfo,
            cancellationToken
        );
    }
}

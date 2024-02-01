using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using SolidTUS.Handlers;
using SolidTUS.Models;
using SolidTUS.ProtocolHandlers;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;

namespace SolidTUS.ProtocolFlows;

/// <summary>
/// The upload flow
/// </summary>
internal class UploadFlow
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
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Either an error or a request context</returns>
    public async ValueTask<Result<TusResult, HttpError>> GetUploadStatusAsync(TusResult context, string fileId, CancellationToken cancellationToken)
    {
        context = HeadRequestHandler.SetResponseCacheControl(context);
        var requestContext = await common.SetUploadFileInfoAsync(context, fileId, cancellationToken);

        requestContext = requestContext
            .Map(CommonRequestHandler.SetTusResumableHeader)
            .Map(HeadRequestHandler.SetUploadOffsetHeader)
            .Map(HeadRequestHandler.SetUploadLengthOrDeferred)
            .Map(HeadRequestHandler.SetMetadataHeader)
            .Bind(ConcatenationRequestHandler.SetPartialMode)
            .Bind(ConcatenationRequestHandler.SetPartialFinalUrls)
            .Map(ExpirationRequestHandler.SetExpiration);

        return requestContext;
    }

    /// <summary>
    /// Called before upload starts. Checks and validates request.
    /// </summary>
    /// <param name="context">The request context</param>
    /// <param name="fileId">The file ID</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Either an error or a request context</returns>
    public async ValueTask<Result<TusResult, HttpError>> PreUploadAsync(TusResult context, string fileId, CancellationToken cancellationToken)
    {
        var requestContext = await common.SetUploadFileInfoAsync(context, fileId, cancellationToken);
        requestContext = await requestContext.Bind(PatchRequestHandler.CheckContentType)
            .Bind(PatchRequestHandler.CheckUploadOffset)
            .Bind(ConcatenationRequestHandler.SetPartialMode)
            .Bind(PatchRequestHandler.CheckConsistentByteOffset)
            .Bind(PatchRequestHandler.CheckUploadLength)
            .Bind(PatchRequestHandler.CheckUploadExceedsFileSize)
            .Bind(checksumHandler.SetChecksum)
            .Bind(async c => await expirationRequestHandler.CheckExpirationAsync(c, cancellationToken));

        return requestContext;
    }

    /// <summary>
    /// Called after an upload finishes, before headers are sent.
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>A collection of headers</returns>
    public static TusResult PostUpload(TusResult context)
    {
        context = ExpirationRequestHandler.SetExpiration(context);
        CommonRequestHandler.SetUploadByteOffset(context);
        CommonRequestHandler.SetTusResumableHeader(context);
        return context;
    }
}

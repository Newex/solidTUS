using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SolidTUS.Contexts;
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
    public async ValueTask<Result<RequestContext>> PreUploadAsync(RequestContext context, string fileId)
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
        var checksum = uploadUpdatedDate.Bind(checksumHandler.SetChecksum);
        return checksum;
    }

    /// <summary>
    /// A PATCH request called just prior sending response headers
    /// </summary>
    /// <param name="context">The request context</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A collection of headers</returns>
    public async Task<IHeaderDictionary> PostUploadAsync(RequestContext context, CancellationToken cancellationToken)
    {
        context = common.SetUpdatedDate(context);
        context = expirationRequestHandler.SetExpiration(context);
        context = CommonRequestHandler.SetUploadByteOffset(context);
        await uploadMetaHandler.UpdateResourceAsync(context.UploadFileInfo, cancellationToken);
        return context.ResponseHeaders;
    }

    /// <summary>
    /// Create upload context from the request context
    /// </summary>
    /// <param name="context">The request context</param>
    /// <param name="reader">The pipe reader</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>An upload context or null</returns>
    public TusUploadContext? CreateUploadContext(Result<RequestContext> context, PipeReader reader, CancellationToken cancellationToken)
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
            uploadFileInfo,
            cancellationToken
        );
    }
}

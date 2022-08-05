using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using SolidTUS.Constants;
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
    private readonly IUploadStorageHandler uploadStorageHandler;

    /// <summary>
    /// Instantiate a new object of <see cref="UploadFlow"/>
    /// </summary>
    /// <param name="commonRequestHandler">The common request handler</param>
    /// <param name="patchRequestHandler">The patch request handler</param>
    /// <param name="checksumRequestHandler"></param>
    /// <param name="uploadStorageHandler">The upload storage handler</param>
    public UploadFlow(
        CommonRequestHandler commonRequestHandler,
        PatchRequestHandler patchRequestHandler,
        ChecksumRequestHandler checksumRequestHandler,
        IUploadStorageHandler uploadStorageHandler
    )
    {
        common = commonRequestHandler;
        patch = patchRequestHandler;
        checksumHandler = checksumRequestHandler;
        this.uploadStorageHandler = uploadStorageHandler;
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
        // Core protocol, start -->
        context.FileID = fileId;
        var contentType = PatchRequestHandler.CheckContentType(context);
        var uploadOffset = contentType.Bind(c => PatchRequestHandler.CheckUploadOffset(c));
        var uploadInfoExists = await uploadOffset.BindAsync(async c => await common.CheckUploadFileInfoExistsAsync(c));
        var byteOffset = uploadInfoExists.Bind(c => PatchRequestHandler.CheckConsistentByteOffset(c));
        var uploadLength = await byteOffset.BindAsync(async c => await patch.CheckUploadLengthAsync(c));
        var uploadSize = uploadLength.Bind(c => PatchRequestHandler.CheckUploadExceedsFileSize(c));
        // <-- end

        return uploadSize;
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
    public TusUploadContext? CreateUploadContext(Result<RequestContext> context, PipeReader reader, Action<long> onDone, Action<HttpError> onError, CancellationToken cancellationToken)
    {
        var requestContext = context.Match(c => c, _ => null!);
        if (requestContext is null)
        {
            return null;
        }

        var contentLength = requestContext.RequestHeaders.ContentLength;
        var uploadFileInfo = requestContext!.UploadFileInfo;
        return new TusUploadContext(
            requestContext.ChecksumContext,
            contentLength,
            uploadStorageHandler,
            reader,
            onDone,
            onError,
            uploadFileInfo,
            cancellationToken
        );
    }
}

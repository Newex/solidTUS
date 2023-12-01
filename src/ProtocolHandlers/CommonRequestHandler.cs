using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Internal;
using SolidTUS.Constants;
using SolidTUS.Contexts;
using SolidTUS.Handlers;
using SolidTUS.Models;
using SolidTUS.Validators;
using static SolidTUS.Extensions.FunctionalExtensions;

namespace SolidTUS.ProtocolHandlers;

/// <summary>
/// General TUS request handler
/// </summary>
internal class CommonRequestHandler
{
    private readonly IUploadStorageHandler uploadStorageHandler;
    private readonly IUploadMetaHandler uploadMetaHandler;
    private readonly ISystemClock clock;

    /// <summary>
    /// Instantiate a new object of <see cref="CommonRequestHandler"/>
    /// </summary>
    /// <param name="uploadStorageHandler"></param>
    /// <param name="uploadMetaHandler">The upload meta handler</param>
    /// <param name="clock">The clock provider</param>
    public CommonRequestHandler(
        IUploadStorageHandler uploadStorageHandler,
        IUploadMetaHandler uploadMetaHandler,
        ISystemClock clock
    )
    {
        this.uploadStorageHandler = uploadStorageHandler;
        this.uploadMetaHandler = uploadMetaHandler;
        this.clock = clock;
    }

    /// <summary>
    /// Check if a <see cref="UploadFileInfo"/> resource has been created
    /// </summary>
    /// <param name="context">The request context</param>
    /// <param name="fileId">The file id</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Either an error or a request context</returns>
    public async ValueTask<Result<TusResult>> SetUploadFileInfoAsync(TusResult context, string fileId, CancellationToken cancellationToken)
    {
        var fileInfo = await uploadMetaHandler.GetResourceAsync(fileId, cancellationToken);

        if (fileInfo is null)
        {
            return HttpError.NotFound("File resource does not exists").Wrap();
        }

        context.UploadFileInfo = fileInfo;
        return context.Wrap();
    }

    /// <summary>
    /// Check if the TUS version is supported by the server
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an http error or a request context</returns>
    public static Result<TusResult> CheckTusVersion(TusResult context)
    {
        if (context.Method == "OPTIONS")
        {
            return context.Wrap();
        }

        var isSupported = TusVersionValidator.IsValidVersion(context.RequestHeaders[TusHeaderNames.Resumable]);
        if (!isSupported)
        {
            var error = HttpError.PreconditionFailed();

            error.Headers.Append(TusHeaderNames.Version, TusHeaderValues.TusServerVersions);
            return error.Wrap();
        }

        return context.Wrap();
    }

    /// <summary>
    /// Set the upload offset header
    /// </summary>
    /// <param name="context">The response context</param>
    /// <returns>A request context</returns>
    public static void SetUploadByteOffset(TusResult context)
    {
        if (context.UploadFileInfo is not null)
        {
            context.ResponseHeaders.Append(TusHeaderNames.UploadOffset, context.UploadFileInfo?.ByteOffset.ToString());
        }
    }

    /// <summary>
    /// Set the tus resumable response header
    /// </summary>
    /// <param name="context">The response context</param>
    public static TusResult SetTusResumableHeader(TusResult context)
    {
        context.ResponseHeaders.Append(TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion);
        return context;
    }
}
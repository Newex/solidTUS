using System;
using System.Threading.Tasks;
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
public class CommonRequestHandler
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
    /// <returns>Either an error or a request context</returns>
    public ValueTask<Result<RequestContext>> CheckUploadFileInfoExistsAsync(RequestContext context)
    {
        // UploadFileInfo? fileInfo;
        // if (context.PartialMode == PartialMode.Partial)
        // {
        //     fileInfo = await uploadMetaHandler.GetPartialResourceAsync(context.FileID, context.CancellationToken);
        // }
        // else
        // {
        //     fileInfo = await uploadMetaHandler.GetResourceAsync(context.FileID, context.CancellationToken);
        // }

        // if (fileInfo is null)
        // {
        //     return HttpError.NotFound("File resource does not exists").Wrap();
        // }

        // var size = uploadStorageHandler.GetUploadSize(context.FileID, fileInfo);
        // fileInfo.ByteOffset = size ?? 0L;
        // context.UploadFileInfo = fileInfo;
        // return context.Wrap();
        throw new NotImplementedException();
    }

    /// <summary>
    /// Check if the TUS version is supported by the server
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an http error or a request context</returns>
    public static Result<RequestContext> CheckTusVersion(RequestContext context)
    {
        if (context.Method == "OPTIONS")
        {
            return context.Wrap();
        }

        var isSupported = TusVersionValidator.IsValidVersion(context.RequestHeaders[TusHeaderNames.Resumable]);
        if (!isSupported)
        {
            var error = HttpError.PreconditionFailed();

            error.Headers.Add(TusHeaderNames.Version, TusHeaderValues.TusServerVersions);
            return error.Request();
        }

        return context.Wrap();
    }

    /// <summary>
    /// Set the upload offset header
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>A request context</returns>
    public static RequestContext SetUploadByteOffset(RequestContext context)
    {
        // context.ResponseHeaders.Add(TusHeaderNames.UploadOffset, context.UploadFileInfo.ByteOffset.ToString());
        // return context;
        throw new NotImplementedException();
    }
}
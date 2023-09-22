using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using SolidTUS.Constants;
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
    /// <remarks>
    /// Sets the <see cref="RequestContext.UploadFileInfo"/> if exists
    /// </remarks>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public async ValueTask<Result<RequestContext>> CheckUploadFileInfoExistsAsync(RequestContext context)
    {
        var fileInfo = await uploadMetaHandler.GetResourceAsync(context.FileID, context.CancellationToken);
        if (fileInfo is null)
        {
            return HttpError.NotFound("File resource does not exists").Wrap();
        }

        var size = uploadStorageHandler.GetUploadSize(context.FileID, fileInfo);
        fileInfo.ByteOffset = size ?? 0L;
        context.UploadFileInfo = fileInfo;
        return context.Wrap();
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
            return error.Wrap();
        }

        return context.Wrap();
    }

    /// <summary>
    /// Set the created date for the upload
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>A request context</returns>
    public RequestContext SetCreatedDateForUpload(RequestContext context)
    {
        var info = context.UploadFileInfo with
        {
            CreatedDate = clock.UtcNow
        };

        return context with
        {
            UploadFileInfo = info
        };
    }
}
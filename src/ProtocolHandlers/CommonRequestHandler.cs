using System.Threading.Tasks;
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

    /// <summary>
    /// Instantiate a new object of <see cref="CommonRequestHandler"/>
    /// </summary>
    /// <param name="uploadStorageHandler"></param>
    /// <param name="uploadMetaHandler">The upload meta handler</param>
    public CommonRequestHandler(
        IUploadStorageHandler uploadStorageHandler,
        IUploadMetaHandler uploadMetaHandler
    )
    {
        this.uploadStorageHandler = uploadStorageHandler;
        this.uploadMetaHandler = uploadMetaHandler;
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
        var fileInfo = await uploadMetaHandler.GetUploadFileInfoAsync(context.FileID, context.CancellationToken);
        if (fileInfo is null)
        {
            return HttpError.NotFound("File resource does not exists").Wrap();
        }

        var size = await uploadStorageHandler.GetUploadSizeAsync(context.FileID, context.CancellationToken, fileInfo.FileDirectoryPath);
        var info = fileInfo with
        {
            ByteOffset = size ?? 0L
        };
        var result = context with
        {
            UploadFileInfo = info
        };

        return result.Wrap();
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
}
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.Net.Http.Headers;
using SolidTUS.Constants;
using SolidTUS.Handlers;
using SolidTUS.Models;
using static SolidTUS.Extensions.FunctionalExtensions;

namespace SolidTUS.ProtocolHandlers;

/// <summary>
/// TUS PATCH request handler
/// </summary>
public class PatchRequestHandler
{
    private readonly IUploadStorageHandler uploadStorageHandler;

    /// <summary>
    /// Instantiate a new object of <see cref="PatchRequestHandler"/>
    /// </summary>
    /// <param name="uploadStorageHandler">The upload storage handler</param>
    public PatchRequestHandler(
        IUploadStorageHandler uploadStorageHandler
    )
    {
        this.uploadStorageHandler = uploadStorageHandler;
    }

    /// <summary>
    /// Check the upload-length header exists and is valid
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public async Task<Either<HttpError, RequestContext>> CheckUploadLengthAsync(RequestContext context)
    {
        var hasSize = context.UploadFileInfo.Match(f => f.FileSize.HasValue, false);
        if (hasSize)
        {
            return context;
        }

        // Must have Upload-Length since it was not supplied during resource creation!
        var givenFileSize = parseLong(context.RequestHeaders[TusHeaderNames.UploadLength]);
        var isSet = await givenFileSize.BindAsync(async size => await uploadStorageHandler.SetFileSizeAsync(context.FileID, size, context.CancellationToken));

        if (!isSet)
        {
            return HttpError.InternalServerError();
        }

        // The given file size must be non zero
        return givenFileSize.Match<Either<HttpError, RequestContext>>(
            s => s > 0 ? context : HttpError.BadRequest("Upload-Length header must have a non-negative value"),
            HttpError.BadRequest("Missing Upload-Length header")
        );
    }

    /// <summary>
    /// Check the request content-type header
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public static Either<HttpError, RequestContext> CheckContentType(RequestContext context)
    {
        var supportMedia = context.RequestHeaders[HeaderNames.ContentType].Equals(TusHeaderValues.PatchContentType);
        if (!supportMedia)
        {
            return HttpError.UnsupportedMediaType();
        }

        return context;
    }

    /// <summary>
    /// Check the upload-offset header
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public static Either<HttpError, RequestContext> CheckUploadOffset(RequestContext context)
    {
        var uploadOffset = parseLong(context.RequestHeaders[TusHeaderNames.UploadOffset]);
        var isValid = from o in uploadOffset
                      select (o >= 0L);

        return match(isValid,
            Some: v => v
                ? Either.Right(context)
                : HttpError.BadRequest("Upload-Offset must have a non-negative value"),
            None: () => HttpError.BadRequest("Missing Upload-Offset header")
        );
    }

    /// <summary>
    /// Check if the upload-offset header value matches the <see cref="UploadFileInfo.ByteOffset"/> value
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public static Either<HttpError, RequestContext> CheckConsistentByteOffset(RequestContext context)
    {
        var uploadOffset = parseLong(context.RequestHeaders[TusHeaderNames.UploadOffset]);
        var fileInfo = context.UploadFileInfo;

        var isValid = from offset in uploadOffset
                      from f in fileInfo
                      select f.ByteOffset == offset;

        return isValid.Match(
            v => v ? Either.Right(context) : HttpError.Conflict("Conflicting file byte offset"),
            HttpError.BadRequest("Missing Upload-Offset header")
        );
    }

    /// <summary>
    /// Check if the current upload size exceeds the specified total upload size
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public static Either<HttpError, RequestContext> CheckUploadExceedsFileSize(RequestContext context)
    {
        var uploadOffset = parseLong(context.RequestHeaders[TusHeaderNames.UploadOffset]);
        var uploadSize = parseLong(context.RequestHeaders[HeaderNames.ContentLength]);
        var fileSize = context.UploadFileInfo.Bind(f => Optional(f.FileSize));

        var isValid = from u in uploadSize
                      from s in fileSize
                      from o in uploadOffset
                      select (u + o <= s);

        return isValid.Match<Either<HttpError, RequestContext>>(
            Some: v => v ? context : HttpError.BadRequest("Data will exceed the specified file size"),
            None: () => HttpError.BadRequest("Missing either Upload-Length header or Content-Length header"));
    }
}
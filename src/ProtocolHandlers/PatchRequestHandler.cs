using Microsoft.Net.Http.Headers;
using SolidTUS.Constants;
using SolidTUS.Models;
using static SolidTUS.Extensions.FunctionalExtensions;

namespace SolidTUS.ProtocolHandlers;

/// <summary>
/// TUS PATCH request handler
/// </summary>
internal class PatchRequestHandler
{
    /// <summary>
    /// Instantiate a new object of <see cref="PatchRequestHandler"/>
    /// </summary>
    public PatchRequestHandler()
    {
    }

    /// <summary>
    /// Check the upload-length header exists and is valid
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public Result<TusResult> CheckUploadLength(TusResult context)
    {
        var hasSize = context.UploadFileInfo?.FileSize.HasValue ?? false;
        if (hasSize)
        {
            return context.Wrap();
        }

        // Must have Upload-Length since it was not supplied during resource creation!
        var hasGivenFileSize = long.TryParse(context.RequestHeaders[TusHeaderNames.UploadLength], out var size);
        if (!hasGivenFileSize)
        {
            return HttpError.BadRequest("Missing Upload-Length header").Wrap();
        }

        // The given file size must be non zero
        var isValid = size > 0;
        if (!isValid)
        {
            return HttpError.BadRequest("Upload-Length header must have a non-negative value").Wrap();
        }

        context.FileSize = size;
        return context.Wrap();
    }

    /// <summary>
    /// Check the request content-type header
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public static Result<TusResult> CheckContentType(TusResult context)
    {
        var supportMedia = context.RequestHeaders[HeaderNames.ContentType].Equals(TusHeaderValues.PatchContentType);
        if (!supportMedia)
        {
            return HttpError.UnsupportedMediaType().Wrap();
        }

        return context.Wrap();
    }

    /// <summary>
    /// Check the upload-offset header
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public static Result<TusResult> CheckUploadOffset(TusResult context)
    {
        var hasUploadOffset = long.TryParse(context.RequestHeaders[TusHeaderNames.UploadOffset], out var uploadOffset);
        if (!hasUploadOffset)
        {
            return HttpError.BadRequest("Missing Upload-Offset header").Wrap();
        }

        if (uploadOffset < 0)
        {
            return HttpError.BadRequest("Upload-Offset must have a non-negative value").Wrap();
        }

        return context.Wrap();
    }

    /// <summary>
    /// Check if the upload-offset header value matches the <see cref="UploadFileInfo.ByteOffset"/> value
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public static Result<TusResult> CheckConsistentByteOffset(TusResult context)
    {
        var hasUploadOffset = long.TryParse(context.RequestHeaders[TusHeaderNames.UploadOffset], out var uploadOffset);
        var fileInfo = context.UploadFileInfo;

        if (!hasUploadOffset)
        {
            return HttpError.BadRequest("Missing Upload-Offset header").Wrap();
        }

        var isValid = fileInfo?.ByteOffset == uploadOffset;
        if (!isValid)
        {
            return HttpError.Conflict("Conflicting file byte offset").Wrap();
        }

        return context.Wrap();
    }

    /// <summary>
    /// Check if the current upload size exceeds the specified total upload size
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public static Result<TusResult> CheckUploadExceedsFileSize(TusResult context)
    {
        var hasUploadOffset = long.TryParse(context.RequestHeaders[TusHeaderNames.UploadOffset], out var uploadOffset);
        var uploadSize = context.RequestHeaders.ContentLength;
        var fileSize = context.FileSize;
        var hasHeaders = hasUploadOffset && uploadSize.HasValue && fileSize.HasValue;
        if (!hasHeaders)
        {
            return HttpError.BadRequest("Missing either Upload-Length header or Content-Length header").Wrap();
        }

        var isValid = uploadSize + uploadOffset <= fileSize;
        if (!isValid)
        {
            return HttpError.BadRequest("Data will exceed the specified file size").Wrap();
        }

        return context.Wrap();
    }
}
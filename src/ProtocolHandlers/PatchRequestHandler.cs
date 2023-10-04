using Microsoft.Net.Http.Headers;
using SolidTUS.Constants;
using SolidTUS.Models;
using static SolidTUS.Extensions.FunctionalExtensions;

namespace SolidTUS.ProtocolHandlers;

/// <summary>
/// TUS PATCH request handler
/// </summary>
public class PatchRequestHandler
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
    public Result<RequestContext> CheckUploadLength(RequestContext context)
    {
        var hasSize = context.UploadFileInfo.FileSize.HasValue;
        if (hasSize)
        {
            return context.Wrap();
        }

        // Must have Upload-Length since it was not supplied during resource creation!
        var hasGivenFileSize = long.TryParse(context.RequestHeaders[TusHeaderNames.UploadLength], out var size);
        if (!hasGivenFileSize)
        {
            return HttpError.BadRequest("Missing Upload-Length header").Request();
        }

        // The given file size must be non zero
        var isValid = size > 0;
        if (!isValid)
        {
            return HttpError.BadRequest("Upload-Length header must have a non-negative value").Request();
        }

        return context.Wrap();
    }

    /// <summary>
    /// Check the request content-type header
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public static Result<RequestContext> CheckContentType(RequestContext context)
    {
        var supportMedia = context.RequestHeaders[HeaderNames.ContentType].Equals(TusHeaderValues.PatchContentType);
        if (!supportMedia)
        {
            return HttpError.UnsupportedMediaType().Request();
        }

        return context.Wrap();
    }

    /// <summary>
    /// Check the upload-offset header
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public static Result<RequestContext> CheckUploadOffset(RequestContext context)
    {
        var hasUploadOffset = long.TryParse(context.RequestHeaders[TusHeaderNames.UploadOffset], out var uploadOffset);
        if (!hasUploadOffset)
        {
            return HttpError.BadRequest("Missing Upload-Offset header").Request();
        }

        if (uploadOffset < 0)
        {
            return HttpError.BadRequest("Upload-Offset must have a non-negative value").Request();
        }

        return context.Wrap();
    }

    /// <summary>
    /// Check if the upload-offset header value matches the <see cref="UploadFileInfo.ByteOffset"/> value
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public static Result<RequestContext> CheckConsistentByteOffset(RequestContext context)
    {
        var hasUploadOffset = long.TryParse(context.RequestHeaders[TusHeaderNames.UploadOffset], out var uploadOffset);
        var fileInfo = context.UploadFileInfo;

        if (!hasUploadOffset)
        {
            return HttpError.BadRequest("Missing Upload-Offset header").Request();
        }

        var isValid = fileInfo.ByteOffset == uploadOffset;
        if (!isValid)
        {
            return HttpError.Conflict("Conflicting file byte offset").Request();
        }

        return context.Wrap();
    }

    /// <summary>
    /// Check if the current upload size exceeds the specified total upload size
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public static Result<RequestContext> CheckUploadExceedsFileSize(RequestContext context)
    {
        var hasUploadOffset = long.TryParse(context.RequestHeaders[TusHeaderNames.UploadOffset], out var uploadOffset);
        var uploadSize = context.RequestHeaders.ContentLength;
        var fileSize = context.UploadFileInfo.FileSize;
        var hasHeaders = hasUploadOffset && uploadSize.HasValue && fileSize.HasValue;
        if (!hasHeaders)
        {
            return HttpError.BadRequest("Missing either Upload-Length header or Content-Length header").Request();
        }

        var isValid = uploadSize + uploadOffset <= fileSize;
        if (!isValid)
        {
            return HttpError.BadRequest("Data will exceed the specified file size").Request();
        }

        return context.Wrap();
    }
}
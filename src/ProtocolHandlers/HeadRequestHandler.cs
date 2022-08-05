using Microsoft.Net.Http.Headers;
using SolidTUS.Constants;
using SolidTUS.Models;

namespace SolidTUS.ProtocolHandlers;

/// <summary>
/// TUS HEAD request handler
/// </summary>
public static class HeadRequestHandler
{
    /// <summary>
    /// Set the response cache control header to no-store
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public static RequestContext SetResponseCacheControl(RequestContext context)
    {
        context.ResponseHeaders.Add(HeaderNames.CacheControl, "no-store");
        return context;
    }

    /// <summary>
    /// Set the response headers depending on if file info resource exists
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public static RequestContext SetUploadLengthOrDeferred(RequestContext context)
    {
        var hasFileSize = context.UploadFileInfo.FileSize.HasValue;
        if (!hasFileSize)
        {
            context.ResponseHeaders.Add(TusHeaderNames.UploadDeferLength, "1");
        }
        else
        {
            context.ResponseHeaders.Add(TusHeaderNames.UploadLength, context.UploadFileInfo.FileSize!.Value.ToString());
        }

        return context;
    }

    /// <summary>
    /// Set the raw metadata header if exists
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>A request context</returns>
    public static RequestContext SetMetadataHeader(RequestContext context)
    {
        context.ResponseHeaders.Add(TusHeaderNames.UploadMetadata, context.UploadFileInfo.RawMetadata);
        return context;
    }

    /// <summary>
    /// Set the upload offset header
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public static RequestContext SetUploadOffsetHeader(RequestContext context)
    {
        var file = context.UploadFileInfo;
        var offset = file.ByteOffset.ToString();

        context.ResponseHeaders.Add(TusHeaderNames.UploadOffset, offset);
        return context;
    }
}

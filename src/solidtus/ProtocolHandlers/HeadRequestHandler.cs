using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using SolidTUS.Constants;
using SolidTUS.Models;

namespace SolidTUS.ProtocolHandlers;

/// <summary>
/// TUS HEAD request handler
/// </summary>
internal static class HeadRequestHandler
{
    /// <summary>
    /// Set the response cache control header to no-store
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public static TusResult SetResponseCacheControl(TusResult context)
    {
        context.ResponseHeaders.Append(HeaderNames.CacheControl, "no-store");
        return context;
    }

    /// <summary>
    /// Set the response headers depending on if file info resource exists
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public static TusResult SetUploadLengthOrDeferred(TusResult context)
    {
        var size = context.UploadFileInfo?.FileSize ?? context.FileSize;
        if (!size.HasValue)
        {
            context.ResponseHeaders.Append(TusHeaderNames.UploadDeferLength, "1");
        }
        else
        {
            context.FileSize = size;
            context.ResponseHeaders.Append(TusHeaderNames.UploadLength, context.FileSize.GetValueOrDefault().ToString());
        }

        return context;
    }

    /// <summary>
    /// Set the raw metadata header if exists
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>A request context</returns>
    public static TusResult SetMetadataHeader(TusResult context)
    {
        context.ResponseHeaders.Append(TusHeaderNames.UploadMetadata, context.UploadFileInfo?.RawMetadata);
        return context;
    }

    /// <summary>
    /// Set the upload offset header
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public static TusResult SetUploadOffsetHeader(TusResult context)
    {
        var file = context.UploadFileInfo;
        var offset = file?.ByteOffset.ToString();

        context.ResponseHeaders.Append(TusHeaderNames.UploadOffset, offset);
        return context;
    }
}

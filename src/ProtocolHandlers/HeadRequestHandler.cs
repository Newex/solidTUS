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
        var fileSize = context.UploadFileInfo.Bind(
            file => Optional(file.FileSize)
        );

        var response = fileSize.Match(
            size => (TusHeaderNames.UploadLength, size.ToString()),
            (TusHeaderNames.UploadDeferLength, "1")
        );

        context.ResponseHeaders.Add(response.Item1, response.Item2);
        return context;
    }

    /// <summary>
    /// Set the raw metadata header if exists
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public static RequestContext SetMetadataHeader(RequestContext context)
    {
        var metadata = context.UploadFileInfo.Bind(
            file => Optional(file.RawMetadata)
        );

        metadata.IfSome(m => context.ResponseHeaders.Add(TusHeaderNames.UploadMetadata, m));
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
        var offset = from f in file
                     select f.ByteOffset.ToString();

        offset.IfSome(o =>
            context.ResponseHeaders.Add(TusHeaderNames.UploadOffset, o)
        );

        return context;
    }
}

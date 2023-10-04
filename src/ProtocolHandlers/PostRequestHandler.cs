using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using SolidTUS.Constants;
using SolidTUS.Contexts;
using SolidTUS.Models;
using SolidTUS.Options;
using SolidTUS.Parsers;
using static SolidTUS.Extensions.FunctionalExtensions;

namespace SolidTUS.ProtocolHandlers;

/// <summary>
/// TUS POST handler
/// </summary>
public class PostRequestHandler
{
    private readonly bool validatePartial;
    private readonly Func<IReadOnlyDictionary<string, string>, bool> metadataValidator;
    private readonly long? maxSize;

    /// <summary>
    /// Instantiate a new object of <see cref="PostRequestHandler"/>
    /// </summary>
    /// <param name="options">The TUS options</param>
    public PostRequestHandler(
        IOptions<TusOptions> options
    )
    {
        metadataValidator = options.Value.MetadataValidator;
        maxSize = options.Value.MaxSize;
        validatePartial = options.Value.ValidateMetadataForParallelUploads;
    }

    /// <summary>
    /// Check that at least one Upload-Length and Upload-Defer-Length headers is present but no both
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public static Result<RequestContext> CheckUploadLengthOrDeferred(RequestContext context)
    {
        var hasDefer = context.RequestHeaders.ContainsKey(TusHeaderNames.UploadDeferLength);
        var hasLength = context.RequestHeaders.ContainsKey(TusHeaderNames.UploadLength);

        if (!(hasDefer ^ hasLength))
        {
            return HttpError.BadRequest("Must have either Upload-Length or Upload-Defer-Length header and not both").Request();
        }

        var isDefer = int.TryParse(context.RequestHeaders[TusHeaderNames.UploadDeferLength], out var defer);
        if (isDefer && defer != 1)
        {
            return HttpError.BadRequest("Invalid Upload-Defer-Length header").Request();
        }
        else if (isDefer)
        {
            return context.Wrap();
        }

        var isLength = long.TryParse(context.RequestHeaders[TusHeaderNames.UploadLength], out var length);
        if (!isLength || length <= 0)
        {
            return HttpError.BadRequest("Invalid Upload-Length header").Request();
        }

        return context.Wrap();
    }

    /// <summary>
    /// Check if the upload exceeds the server defined single file upload limit
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public Result<RequestContext> CheckMaximumSize(RequestContext context)
    {
        var hasHeader = long.TryParse(context.RequestHeaders[TusHeaderNames.UploadLength], out var size);
        if (!hasHeader)
        {
            return HttpError.BadRequest("Missing Upload-Length header").Request();
        }

        if (maxSize is not null)
        {
            var allowed = size <= maxSize.Value;
            if (!allowed)
            {
                var error = HttpError.EntityTooLarge("File upload is bigger than server restrictions");
                error.Headers.Add(TusHeaderNames.MaxSize, maxSize.Value.ToString());
                return error.Request();
            }
        }

        return context.Wrap();
    }

    /// <summary>
    /// Parse metadata from a request
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>A tuple containing the raw string metadata and the parsed metadata</returns>
    public static RequestContext ParseMetadata(RequestContext context)
    {
        var rawMetadata = context.RequestHeaders[TusHeaderNames.UploadMetadata];
        if (rawMetadata.Count == 0)
        {
            return context;
        }

        var metadata = MetadataParser.ParseFast(rawMetadata!);
        context.Metadata = metadata.AsReadOnly();
        context.RawMetadata = rawMetadata;
        return context;
    }

    /// <summary>
    /// Validate the metadata
    /// </summary>
    /// <param name="context">THe request context</param>
    /// <returns>Either an error or a request context</returns>
    public Result<RequestContext> ValidateMetadata(RequestContext context)
    {
        var isPartial = context.PartialMode == PartialMode.Partial;
        if (validatePartial || !isPartial)
        {
            var metadata = context.Metadata ?? new Dictionary<string, string>();
            var isValid = metadataValidator(metadata);
            return isValid ? context.Wrap() : HttpError.BadRequest("Invalid Upload-Metadata").Request();
        }

        return context.Wrap();
    }

    /// <summary>
    /// Set the total file size for the upload
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>An updated context with file size</returns>
    public static RequestContext SetFileSize(RequestContext context)
    {
        var hasLength = long.TryParse(context.RequestHeaders[TusHeaderNames.UploadLength], out var size);
        if (!hasLength)
        {
            return context;
        }

        context.FileSize = size;
        return context;
    }

    /// <summary>
    /// Check if the request has upload data and that it conforms to TUS-protocol
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public static Result<RequestContext> CheckIsValidUpload(RequestContext context)
    {
        var hasContentLength = long.TryParse(context.RequestHeaders[HeaderNames.ContentLength], out var contentLength);
        var isUpload = hasContentLength && contentLength > 0;
        if (!isUpload)
        {
            // Does not contain upload data
            return context.Wrap();
        }

        var contentType = context.RequestHeaders[HeaderNames.ContentType];
        var isValid = string.Equals(contentType, TusHeaderValues.PatchContentType, StringComparison.OrdinalIgnoreCase);
        if (!isValid)
        {
            return HttpError.BadRequest("Must include proper Content-Type header value: application/offset+octet-stream").Request();
        }

        return context.Wrap();
    }

    /// <summary>
    /// Set the maximum file size header if present
    /// </summary>
    /// <param name="context">The response context</param>
    public void SetMaximumFileSize(ResponseContext context)
    {
        if (maxSize.HasValue)
        {
            context.ResponseHeaders.Add(TusHeaderNames.MaxSize, maxSize.Value.ToString());
        }
    }
}

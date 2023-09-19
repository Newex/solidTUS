using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using SolidTUS.Constants;
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
    private readonly Func<Dictionary<string, string>, bool> metadataValidator;
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
            return HttpError.BadRequest("Must have either Upload-Length or Upload-Defer-Length header and not both").Wrap();
        }

        var isDefer = int.TryParse(context.RequestHeaders[TusHeaderNames.UploadDeferLength], out var defer);
        if (isDefer && defer != 1)
        {
            return HttpError.BadRequest("Invalid Upload-Defer-Length header").Wrap();
        }
        else if (isDefer)
        {
            return context.Wrap();
        }

        var isLength = long.TryParse(context.RequestHeaders[TusHeaderNames.UploadLength], out var length);
        if (!isLength || length <= 0)
        {
            return HttpError.BadRequest("Invalid Upload-Length header").Wrap();
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
            return HttpError.BadRequest("Missing Upload-Length header").Wrap();
        }

        if (maxSize is not null)
        {
            var allowed = size <= maxSize.Value;
            if (!allowed)
            {
                return HttpError.EntityTooLarge("File upload is bigger than server restrictions").Wrap();
            }
        }

        return context.Wrap();
    }

    /// <summary>
    /// Parse metadata from a request
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>A tuple containing the raw string metadata and the parsed metadata</returns>
    public static (StringValues Raw, Dictionary<string, string> Parsed) ParseMetadata(RequestContext context)
    {
        var rawMetadata = context.RequestHeaders[TusHeaderNames.UploadMetadata];
        var metadata = MetadataParser.ParseFast(rawMetadata!);
        return (rawMetadata, metadata);
    }

    /// <summary>
    /// Validate the metadata
    /// </summary>
    /// <param name="context">THe request context</param>
    /// <param name="metadata">The parsed metadata</param>
    /// <returns>Either an error or a request context</returns>
    public Result<RequestContext> ValidateMetadata(RequestContext context, Dictionary<string, string> metadata)
    {
        var isValid = metadataValidator(metadata);
        return isValid ? context.Wrap() : HttpError.BadRequest("Invalid Upload-Metadata").Wrap();
    }

    /// <summary>
    /// Set the metadata into the contexts UploadFileInfo
    /// </summary>
    /// <param name="context">The request context</param>
    /// <param name="metadata">The tuple of raw and parsed metadata</param>
    /// <returns>Either an error or a request context</returns>
    public static RequestContext SetNewMetadata(RequestContext context, (StringValues, Dictionary<string, string>) metadata)
    {
        var uploadInfo = context.UploadFileInfo;
        var update = uploadInfo with
        {
            RawMetadata = metadata.Item1,
            Metadata = metadata.Item2.ToImmutableDictionary()
        };

        return context with
        {
            UploadFileInfo = update
        };
    }

    /// <summary>
    /// Set the total file size for the upload
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>An updated context with file size</returns>
    public static RequestContext SetFileSize(RequestContext context)
    {
        var info = context.UploadFileInfo;
        var hasLength = long.TryParse(context.RequestHeaders[TusHeaderNames.UploadLength], out var size);
        if (!hasLength)
        {
            return context;
        }

        var update = info with
        {
            FileSize = size
        };

        return context with
        {
            UploadFileInfo = update
        };
    }

    /// <summary>
    /// Check if the request has upload data and that it conforms to TUS-protocol
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public static Result<RequestContext> CheckIsValidUpload(RequestContext context)
    {
        var contentType = context.RequestHeaders[HeaderNames.ContentType];

        var isValid = string.Equals(contentType, TusHeaderValues.PatchContentType, StringComparison.OrdinalIgnoreCase);
        if (!isValid)
        {
            return HttpError.BadRequest("Must include proper Content-Type header value: application/offset+octet-stream").Wrap();
        }

        return context.Wrap();
    }
}

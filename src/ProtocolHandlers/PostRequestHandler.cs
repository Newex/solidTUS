using System;
using System.Collections.Generic;
using LanguageExt;
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
    private readonly Option<long> maxSize;

    /// <summary>
    /// Instantiate a new object of <see cref="PostRequestHandler"/>
    /// </summary>
    /// <param name="options">The TUS options</param>
    public PostRequestHandler(
        IOptions<TusOptions> options
    )
    {
        metadataValidator = options.Value.MetadataValidator;
        maxSize = Optional(options.Value.MaxSize);
    }

    /// <summary>
    /// Check that at least one Upload-Length and Upload-Defer-Length headers is present but no both
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public static Either<HttpError, RequestContext> CheckUploadLengthOrDeferred(RequestContext context)
    {
        var hasDefer = context.RequestHeaders.ContainsKey(TusHeaderNames.UploadDeferLength);
        var hasLength = context.RequestHeaders.ContainsKey(TusHeaderNames.UploadLength);

        if (!(hasDefer ^ hasLength))
        {
            return HttpError.BadRequest("Must have either Upload-Length or Upload-Defer-Length header and not both");
        }

        var defer = parseInt(context.RequestHeaders[TusHeaderNames.UploadDeferLength]);
        var validDefer = defer.Bind(v => v == 1 ? Some(true) : None).Match<Either<HttpError, RequestContext>>(
            Some: _ => context,
            None: HttpError.BadRequest("Invalid Upload-Defer-Length header")
        );

        if (hasDefer)
        {
            return validDefer;
        }

        var length = parseLong(context.RequestHeaders[TusHeaderNames.UploadLength]);
        return length.Bind(l => l > 0 ? Some(true) : None).Match<Either<HttpError, RequestContext>>(
            Some: _ => context,
            None: HttpError.BadRequest("Invalid Upload-Length header")
        );
    }

    /// <summary>
    /// Check if the upload exceeds the server defined single file upload limit
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public Either<HttpError, RequestContext> CheckMaximumSize(RequestContext context)
    {
        var size = parseLong(context.RequestHeaders[TusHeaderNames.UploadLength]);
        var isValid = from s in size
                      from m in maxSize
                      select (s <= m);

        return isValid.Match(
            v => v ? Either.Right(context) : HttpError.EntityTooLarge("File upload is bigger than server restrictions"),
            context // MaxSize is None (What if Size is None?)
        );
    }

    /// <summary>
    /// Parse metadata from a request
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>A tuple containing the raw string metadata and the parsed metadata</returns>
    public static (StringValues Raw, Dictionary<string, string> Parsed) ParseMetadata(RequestContext context)
    {
        var rawMetadata = context.RequestHeaders[TusHeaderNames.UploadMetadata];
        var metadata = MetadataParser.Parse(rawMetadata);
        return (rawMetadata, metadata);
    }

    /// <summary>
    /// Validate the metadata
    /// </summary>
    /// <param name="context">THe request context</param>
    /// <param name="metadata">The parsed metadata</param>
    /// <returns>Either an error or a request context</returns>
    public Either<HttpError, RequestContext> ValidateMetadata(RequestContext context, Dictionary<string, string> metadata)
    {
        var isValid = metadataValidator(metadata);
        return isValid ? context : HttpError.BadRequest("Invalid Upload-Metadata");
    }

    /// <summary>
    /// Set the metadata into the contexts UploadFileInfo
    /// </summary>
    /// <param name="context">The request context</param>
    /// <param name="metadata">The tuple of raw and parsed metadata</param>
    /// <returns>Either an error or a request context</returns>
    public static RequestContext SetNewMetadata(RequestContext context, (StringValues, Dictionary<string, string>) metadata)
    {
        var info = context.UploadFileInfo;
        var uploadInfo = info.IfNone(() => new());
        var update = uploadInfo with
        {
            RawMetadata = metadata.Item1,
            Metadata = metadata.Item2
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
        var info = context.UploadFileInfo.IfNone(() => new());
        var size = parseLong(context.RequestHeaders[TusHeaderNames.UploadLength]).MatchUnsafe<long?>(
            Some: s => s,
            None: () => null
        );
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
    public static Either<HttpError, RequestContext> CheckIsValidUpload(RequestContext context)
    {
        var contentLength = parseLong(context.RequestHeaders[HeaderNames.ContentLength]);
        var contentType = Optional(context.RequestHeaders[HeaderNames.ContentType]).Map(v => v.ToString());

        var isValid = from size in contentLength
                      from trait in contentType
                      select (size > 0 && trait.Equals(TusHeaderValues.PatchContentType, StringComparison.OrdinalIgnoreCase));

        return isValid.Match(
            Some: b => b ? Either.Right(context) : HttpError.BadRequest("Must include proper Content-Type header value: application/offset+octet-stream"),
            HttpError.BadRequest("Content-Length must be non-negative value ")
        );
    }
}

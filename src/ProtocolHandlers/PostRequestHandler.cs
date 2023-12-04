using System;
using System.Collections.Generic;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using SolidTUS.Constants;
using SolidTUS.Models;
using SolidTUS.Options;
using SolidTUS.Parsers;

namespace SolidTUS.ProtocolHandlers;

/// <summary>
/// TUS POST handler
/// </summary>
internal class PostRequestHandler
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
    public static Result<TusResult, HttpError> CheckUploadLengthOrDeferred(TusResult context)
    {
        if (context.PartialMode == PartialMode.Final)
        {
            // Do not check on final
            return context;
        }

        var hasDefer = context.RequestHeaders.ContainsKey(TusHeaderNames.UploadDeferLength);
        var hasLength = context.RequestHeaders.ContainsKey(TusHeaderNames.UploadLength);

        if (!(hasDefer ^ hasLength))
        {
            return HttpError.BadRequest("Must have either Upload-Length or Upload-Defer-Length header and not both");
        }

        var isDefer = int.TryParse(context.RequestHeaders[TusHeaderNames.UploadDeferLength], out var defer);
        if (isDefer && defer != 1)
        {
            return HttpError.BadRequest("Invalid Upload-Defer-Length header");
        }
        else if (isDefer)
        {
            return context;
        }

        var isLength = long.TryParse(context.RequestHeaders[TusHeaderNames.UploadLength], out var length);
        if (!isLength || length <= 0)
        {
            return HttpError.BadRequest("Invalid Upload-Length header");
        }

        return context;
    }

    /// <summary>
    /// Check if the upload exceeds the server defined single file upload limit
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public Result<TusResult, HttpError> CheckMaximumSize(TusResult context)
    {
        if (context.PartialMode == PartialMode.Final)
        {
            return context;
        }

        var hasHeader = long.TryParse(context.RequestHeaders[TusHeaderNames.UploadLength], out var size);
        if (!hasHeader)
        {
            return HttpError.BadRequest("Missing Upload-Length header");
        }

        if (maxSize is not null)
        {
            var allowed = size <= maxSize.Value;
            if (!allowed)
            {
                var error = HttpError.EntityTooLarge("File upload is bigger than server restrictions");
                error.Headers.Append(TusHeaderNames.MaxSize, maxSize.Value.ToString());
                return error;
            }
        }

        return context;
    }

    /// <summary>
    /// Parse metadata from a request
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>A tuple containing the raw string metadata and the parsed metadata</returns>
    public static TusResult ParseMetadata(TusResult context)
    {
        var rawMetadata = context.RequestHeaders[TusHeaderNames.UploadMetadata];
        if (rawMetadata.Count == 0)
        {
            return context;
        }

        var metadata = MetadataParser.Parse(rawMetadata!);
        context.Metadata = metadata.AsReadOnly();
        context.RawMetadata = rawMetadata;
        return context;
    }

    /// <summary>
    /// Validate the metadata
    /// </summary>
    /// <param name="context">THe request context</param>
    /// <returns>Either an error or a request context</returns>
    public Result<TusResult, HttpError> ValidateMetadata(TusResult context)
    {
        var isPartial = context.PartialMode == PartialMode.Partial;
        if (validatePartial || !isPartial)
        {
            var metadata = context.Metadata ?? new Dictionary<string, string>();
            var isValid = metadataValidator(metadata);
            return Result.SuccessIf(isValid, context, HttpError.BadRequest("Invalid Upload-Metadata"));
        }

        return context;
    }

    /// <summary>
    /// Set the total file size for the upload
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>An updated context with file size</returns>
    public static TusResult SetFileSize(TusResult context)
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
    public static Result<TusResult, HttpError> CheckIsValidUpload(TusResult context)
    {
        var hasContentLength = long.TryParse(context.RequestHeaders[HeaderNames.ContentLength], out var contentLength);
        var isUpload = hasContentLength && contentLength > 0;
        if (!isUpload)
        {
            // Does not contain upload data
            return context;
        }

        var contentType = context.RequestHeaders[HeaderNames.ContentType];
        var isValid = string.Equals(contentType, TusHeaderValues.PatchContentType, StringComparison.OrdinalIgnoreCase);
        if (!isValid)
        {
            return HttpError.BadRequest("Must include proper Content-Type header value: application/offset+octet-stream");
        }

        return context;
    }

    public static TusResult SetCreationLocation(TusResult context)
    {
        var hasLocation = context.LocationUrl is not null;
        if (!hasLocation)
        {
            return context;
        }

        context.ResponseHeaders.Append(HeaderNames.Location, context.LocationUrl);
        return context;
    }

    /// <summary>
    /// Set the maximum file size header if present
    /// </summary>
    /// <param name="context">The response context</param>
    public void SetMaximumFileSize(TusResult context)
    {
        if (maxSize.HasValue)
        {
            context.ResponseHeaders.Append(TusHeaderNames.MaxSize, maxSize.Value.ToString());
        }
    }
}

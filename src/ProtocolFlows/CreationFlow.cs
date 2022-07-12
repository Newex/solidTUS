using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using LanguageExt;
using Microsoft.Net.Http.Headers;
using SolidTUS.Handlers;
using SolidTUS.Models;
using SolidTUS.ProtocolHandlers;

namespace SolidTUS.ProtocolFlows;

/// <summary>
/// Creation flow
/// </summary>
public class CreationFlow
{
    private readonly PostRequestHandler post;
    private readonly IUploadStorageHandler uploadStorageHandler;

    /// <summary>
    /// Instantiate a new object of <see cref="CreationFlow"/>
    /// </summary>
    /// <param name="post">The post request handler</param>
    /// <param name="uploadStorageHandler">The upload storage handler</param>
    public CreationFlow(
        PostRequestHandler post,
        IUploadStorageHandler uploadStorageHandler
    )
    {
        this.post = post;
        this.uploadStorageHandler = uploadStorageHandler;
    }

    /// <summary>
    /// Create resource metadata
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public Either<HttpError, RequestContext> StartResourceCreation(RequestContext context)
    {
        var lengthAndDefer = PostRequestHandler.CheckUploadLengthOrDeferred(context);
        var maxSize = lengthAndDefer.Bind(c => post.CheckMaximumSize(c));

        var parseMetadata = maxSize.Map(c => (Context: c, Metadata: PostRequestHandler.ParseMetadata(c)));
        var validate = parseMetadata.Bind(t =>
        {
            var c = t.Context;
            var m = t.Metadata;
            var valid = post.ValidateMetadata(c, m.Parsed);
            return valid.Map(r => (Context: r, Metadata: m));
        });
        var setMetadata = validate.Map(t => PostRequestHandler.SetNewMetadata(t.Context, t.Metadata));
        var setFileSize = setMetadata.Map(c => PostRequestHandler.SetFileSize(c));

        var contentLength = parseLong(context.RequestHeaders[HeaderNames.ContentLength]);
        var isUpload = (from l in contentLength select l > 0).IfNone(false);
        if (isUpload)
        {
            return setFileSize.Bind(c => PostRequestHandler.CheckIsValidUpload(c));
        }

        return setFileSize;
    }

    /// <summary>
    /// Create a TUS creation context
    /// </summary>
    /// <param name="context">The request context</param>
    /// <param name="reader">The pipeline reader</param>
    /// <param name="onCreated">Callback for when resource has been created</param>
    /// <param name="onUploadPartial">Callback for when data has been uploaded</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A TUS creation context</returns>
    public TusCreationContext? CreateTusContext(Either<HttpError, RequestContext> context, PipeReader reader, Action<string> onCreated, Action<long> onUploadPartial, CancellationToken cancellationToken)
    {
        var requestContext = context.MatchUnsafe(c => c, _ => null);
        if (requestContext is null)
        {
            return null;
        }

        var uploadSize = parseLong(requestContext.RequestHeaders[HeaderNames.ContentLength]).IfNone(0);
        var info = requestContext.UploadFileInfo;
        var rawMetadata = info.Select(f => f.RawMetadata).IfNoneUnsafe(() => null);
        var metadata = info.Select(f => f.Metadata).IfNone(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        var fileSize = info.Bind(f => Optional(f.FileSize)).MatchUnsafe<long?>(s => s, () => null);

        return new TusCreationContext(
            uploadSize > 0,
            rawMetadata,
            metadata,
            fileSize,
            onCreated,
            onUploadPartial,
            reader,
            uploadStorageHandler,
            cancellationToken
        );
    }
}

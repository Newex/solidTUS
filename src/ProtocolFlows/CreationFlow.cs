using System;
using System.IO.Pipelines;
using System.Threading;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using SolidTUS.Contexts;
using SolidTUS.Handlers;
using SolidTUS.Models;
using SolidTUS.Options;
using SolidTUS.ProtocolHandlers;

namespace SolidTUS.ProtocolFlows;

/// <summary>
/// Creation flow
/// </summary>
public class CreationFlow
{
    private readonly IOptions<FileStorageOptions> options;
    private readonly PostRequestHandler post;
    private readonly IUploadStorageHandler uploadStorageHandler;
    private readonly IUploadMetaHandler uploadMetaHandler;

    /// <summary>
    /// Instantiate a new object of <see cref="CreationFlow"/>
    /// </summary>
    /// <param name="options">The file storage options</param>
    /// <param name="post">The post request handler</param>
    /// <param name="uploadStorageHandler">The upload storage handler</param>
    /// <param name="uploadMetaHandler">The upload meta handler</param>
    public CreationFlow(
        IOptions<FileStorageOptions> options,
        PostRequestHandler post,
        IUploadStorageHandler uploadStorageHandler,
        IUploadMetaHandler uploadMetaHandler
    )
    {
        this.options = options;
        this.post = post;
        this.uploadStorageHandler = uploadStorageHandler;
        this.uploadMetaHandler = uploadMetaHandler;
    }

    /// <summary>
    /// Create resource metadata
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public Result<RequestContext> StartResourceCreation(RequestContext context)
    {
        var lengthAndDefer = PostRequestHandler.CheckUploadLengthOrDeferred(context);
        var maxSize = lengthAndDefer.Bind(c => post.CheckMaximumSize(c));

        var parseMetadata = maxSize.Map(c => (Context: c, Metadata: PostRequestHandler.ParseMetadata(c)));
        var validate = parseMetadata.Bind(t =>
        {
            var (ctx, meta) = t;
            var valid = post.ValidateMetadata(ctx, meta.Parsed);
            return valid.Map(c => (Context: c, Metadata: meta));
        });
        var setMetadata = validate.Map(t => PostRequestHandler.SetNewMetadata(t.Context, t.Metadata));
        var setFileSize = setMetadata.Map(PostRequestHandler.SetFileSize);

        var hasContentLength = long.TryParse(context.RequestHeaders[HeaderNames.ContentLength], out var contentLength);
        var isUpload = hasContentLength && contentLength > 0;
        if (isUpload)
        {
            return setFileSize.Bind(PostRequestHandler.CheckIsValidUpload);
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
    public TusCreationContext? CreateTusContext(Result<RequestContext> context, PipeReader reader, Action<string> onCreated, Action<long> onUploadPartial, CancellationToken cancellationToken)
    {
        var requestContext = context.Match(c => c, _ => null!);
        if (requestContext is null)
        {
            return null;
        }

        var uploadSize = requestContext.RequestHeaders.ContentLength;
        var info = requestContext.UploadFileInfo;

        return new TusCreationContext(
            options,
            uploadSize > 0,
            info,
            onCreated,
            onUploadPartial,
            reader,
            uploadStorageHandler,
            uploadMetaHandler,
            cancellationToken
        );
    }
}

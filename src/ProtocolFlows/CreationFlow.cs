using System;
using System.IO.Pipelines;
using System.Threading;

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using SolidTUS.Contexts;
using SolidTUS.Handlers;
using SolidTUS.Models;
using SolidTUS.Options;
using SolidTUS.ProtocolHandlers;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;

namespace SolidTUS.ProtocolFlows;

/// <summary>
/// Creation flow
/// </summary>
public class CreationFlow
{
    private readonly IOptions<FileStorageOptions> options;
    private readonly CommonRequestHandler common;
    private readonly PostRequestHandler post;
    private readonly ExpirationRequestHandler expiration;
    private readonly IUploadStorageHandler uploadStorageHandler;
    private readonly IUploadMetaHandler uploadMetaHandler;
    private readonly LinkGenerator linkGenerator;

    /// <summary>
    /// Instantiate a new object of <see cref="CreationFlow"/>
    /// </summary>
    /// <param name="options">The file storage options</param>
    /// <param name="common">The common request handler</param>
    /// <param name="post">The post request handler</param>
    /// <param name="expiration">The expiration request handler</param>
    /// <param name="uploadStorageHandler">The upload storage handler</param>
    /// <param name="uploadMetaHandler">The upload meta handler</param>
    /// <param name="linkGenerator"></param>
    public CreationFlow(
        IOptions<FileStorageOptions> options,
        CommonRequestHandler common,
        PostRequestHandler post,
        ExpirationRequestHandler expiration,
        IUploadStorageHandler uploadStorageHandler,
        IUploadMetaHandler uploadMetaHandler,
        LinkGenerator linkGenerator
    )
    {
        this.options = options;
        this.common = common;
        this.post = post;
        this.expiration = expiration;
        this.uploadStorageHandler = uploadStorageHandler;
        this.uploadMetaHandler = uploadMetaHandler;
        this.linkGenerator = linkGenerator;
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

        var parseMetadata = maxSize.Map(PostRequestHandler.ParseMetadata);
        var validate = parseMetadata.Bind(post.ValidateMetadata);
        var setFileSize = validate.Map(PostRequestHandler.SetFileSize);
        var setCreatedDate = setFileSize.Map(common.SetCreatedDate);

        var hasContentLength = long.TryParse(context.RequestHeaders[HeaderNames.ContentLength], out var contentLength);
        var isUpload = hasContentLength && contentLength > 0;
        if (isUpload)
        {
            return setCreatedDate.Bind(PostRequestHandler.CheckIsValidUpload);
        }

        return setCreatedDate;
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
            linkGenerator,
            cancellationToken
        );
    }
}

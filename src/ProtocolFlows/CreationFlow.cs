using System;
using System.IO.Pipelines;
using System.Threading;
using Microsoft.AspNetCore.Routing;
using SolidTUS.Contexts;
using SolidTUS.Handlers;
using SolidTUS.Models;
using SolidTUS.ProtocolHandlers;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;

namespace SolidTUS.ProtocolFlows;

/// <summary>
/// Creation flow
/// </summary>
public class CreationFlow
{
    private readonly CommonRequestHandler common;
    private readonly PostRequestHandler post;
    private readonly ExpirationRequestHandler expiration;
    private readonly IUploadStorageHandler uploadStorageHandler;
    private readonly IUploadMetaHandler uploadMetaHandler;
    private readonly LinkGenerator linkGenerator;

    /// <summary>
    /// Instantiate a new object of <see cref="CreationFlow"/>
    /// </summary>
    /// <param name="common">The common request handler</param>
    /// <param name="post">The post request handler</param>
    /// <param name="expiration">The expiration request handler</param>
    /// <param name="uploadStorageHandler">The upload storage handler</param>
    /// <param name="uploadMetaHandler">The upload meta handler</param>
    /// <param name="linkGenerator"></param>
    public CreationFlow(
        CommonRequestHandler common,
        PostRequestHandler post,
        ExpirationRequestHandler expiration,
        IUploadStorageHandler uploadStorageHandler,
        IUploadMetaHandler uploadMetaHandler,
        LinkGenerator linkGenerator
    )
    {
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
        var requestContext = PostRequestHandler
            .CheckUploadLengthOrDeferred(context)
            .Bind(post.CheckMaximumSize)
            .Map(PostRequestHandler.ParseMetadata)
            .Bind(post.ValidateMetadata)
            .Map(PostRequestHandler.SetFileSize)
            .Map(common.SetCreatedDate)
            .Bind(PostRequestHandler.CheckIsValidUpload)
            .Bind(ConcatenationRequestHandler.SetIfUploadIsPartial)
            .Bind(ConcatenationRequestHandler.SetPartialUrlsIfFinal);

        return requestContext;
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
            uploadSize > 0,
            requestContext.PartialMode,
            requestContext.PartialUrls,
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

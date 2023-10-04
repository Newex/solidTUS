using System;
using System.IO.Pipelines;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SolidTUS.Contexts;
using SolidTUS.Handlers;
using SolidTUS.Models;
using SolidTUS.Options;
using SolidTUS.ProtocolHandlers;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;
using SolidTUS.Wrappers;

namespace SolidTUS.ProtocolFlows;

/// <summary>
/// Creation flow
/// </summary>
public class CreationFlow
{
    private readonly CommonRequestHandler common;
    private readonly PostRequestHandler post;
    private readonly ChecksumRequestHandler checksum;
    private readonly IUploadStorageHandler uploadStorageHandler;
    private readonly IUploadMetaHandler uploadMetaHandler;
    private readonly ILinkGeneratorWrapper linkGenerator;
    private readonly IOptions<TusOptions> options;
    private readonly ILogger logger;

    /// <summary>
    /// Instantiate a new object of <see cref="CreationFlow"/>
    /// </summary>
    /// <param name="common">The common request handler</param>
    /// <param name="post">The post request handler</param>
    /// <param name="checksum">The checksum request handler</param>
    /// <param name="uploadStorageHandler">The upload storage handler</param>
    /// <param name="uploadMetaHandler">The upload meta handler</param>
    /// <param name="linkGenerator"></param>
    /// <param name="options">The tus options</param>
    /// <param name="logger">The optional logger</param>
    public CreationFlow(
        CommonRequestHandler common,
        PostRequestHandler post,
        ChecksumRequestHandler checksum,
        IUploadStorageHandler uploadStorageHandler,
        IUploadMetaHandler uploadMetaHandler,
        ILinkGeneratorWrapper linkGenerator,
        IOptions<TusOptions> options,
        ILogger? logger = null
    )
    {
        this.common = common;
        this.post = post;
        this.checksum = checksum;
        this.uploadStorageHandler = uploadStorageHandler;
        this.uploadMetaHandler = uploadMetaHandler;
        this.linkGenerator = linkGenerator;
        this.options = options;
        this.logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Check and set info for the resource creation request
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public Result<RequestContext> PreResourceCreation(RequestContext context)
    {
        var requestContext = PostRequestHandler
            .CheckUploadLengthOrDeferred(context)
            .Bind(ConcatenationRequestHandler.SetPartialMode)
            .Bind(ConcatenationRequestHandler.CheckPartialFinalFormat)
            .Map(PostRequestHandler.SetFileSize)
            .Bind(post.CheckMaximumSize)
            .Map(PostRequestHandler.ParseMetadata)
            .Bind(post.ValidateMetadata)
            .Bind(PostRequestHandler.CheckIsValidUpload)
            .Bind(checksum.SetChecksum);

        return requestContext;
    }

    /// <summary>
    /// TODO on after creation
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public void PostResourceCreation(UploadFileInfo uploadFileInfo, IHeaderDictionary responseHeaders)
    {
        // Set the following
        // 1. Set Upload-Offset header to the size of uploaded bytes
        // 2. Set created date for the file (SolidTus metadata)
        // 3. Set the Tus-Resumable header = 1.0.0
        // 4. Set max size header for response

        throw new NotImplementedException();
    }

    /// <summary>
    /// Create a TUS creation context
    /// </summary>
    /// <param name="context">The request context</param>
    /// <param name="reader">The pipeline reader</param>
    /// <param name="responseHeaders">The response headers</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A TUS creation context</returns>
    public TusCreationContextOLD? CreateTusContext(Result<RequestContext> context, PipeReader reader, IHeaderDictionary responseHeaders, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
        // var requestContext = context.Match(c => c, _ => null!);
        // if (requestContext is null)
        // {
        //     return null;
        // }

        // var uploadSize = requestContext.RequestHeaders.ContentLength;
        // var info = requestContext.UploadFileInfo;

        // return new TusCreationContext(
        //     uploadSize > 0,
        //     requestContext.PartialMode,
        //     requestContext.PartialUrls,
        //     info,
        //     responseHeaders,
        //     reader,
        //     uploadStorageHandler,
        //     uploadMetaHandler,
        //     linkGenerator,
        //     cancellationToken,
        //     options,
        //     logger
        // );
    }
}

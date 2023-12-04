using CSharpFunctionalExtensions;
using SolidTUS.Models;
using SolidTUS.ProtocolHandlers;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;

namespace SolidTUS.ProtocolFlows;

/// <summary>
/// Creation flow
/// </summary>
internal class CreationFlow
{
    private readonly PostRequestHandler post;
    private readonly ChecksumRequestHandler checksum;
    private readonly ExpirationRequestHandler expiration;

    /// <summary>
    /// Instantiate a new object of <see cref="CreationFlow"/>
    /// </summary>
    /// <param name="post">The post request handler</param>
    /// <param name="checksum">The checksum request handler</param>
    /// <param name="expiration">The expiration request handler</param>
    public CreationFlow(
        PostRequestHandler post,
        ChecksumRequestHandler checksum,
        ExpirationRequestHandler expiration
    )
    {
        this.post = post;
        this.checksum = checksum;
        this.expiration = expiration;
    }

    /// <summary>
    /// Check and set info for the resource creation request
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public Result<TusResult, HttpError> PreResourceCreation(TusResult context)
    {
        var requestContext = ConcatenationRequestHandler.SetPartialMode(context)
            .Bind(ConcatenationRequestHandler.SetPartialFinalUrls)
            .Bind(PostRequestHandler.CheckUploadLengthOrDeferred)
            .Map(PostRequestHandler.SetFileSize)
            .Bind(post.CheckMaximumSize)
            .Map(PostRequestHandler.ParseMetadata)
            .Bind(post.ValidateMetadata)
            .Bind(PostRequestHandler.CheckIsValidUpload)
            .Bind(checksum.SetChecksum);

        return requestContext;
    }

    /// <summary>
    /// Post resource creation set response headers
    /// </summary>
    public TusResult PostResourceCreation(TusResult responseContext)
    {
        // Set the following
        CommonRequestHandler.SetUploadByteOffset(responseContext);
        CommonRequestHandler.SetTusResumableHeader(responseContext);
        post.SetMaximumFileSize(responseContext);
        ExpirationRequestHandler.SetExpiration(responseContext);
        PostRequestHandler.SetCreationLocation(responseContext);

        return responseContext;
    }
}

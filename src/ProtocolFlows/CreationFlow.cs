using SolidTUS.Contexts;
using SolidTUS.Models;
using SolidTUS.ProtocolHandlers;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;

namespace SolidTUS.ProtocolFlows;

/// <summary>
/// Creation flow
/// </summary>
public class CreationFlow
{
    private readonly PostRequestHandler post;
    private readonly ChecksumRequestHandler checksum;

    /// <summary>
    /// Instantiate a new object of <see cref="CreationFlow"/>
    /// </summary>
    /// <param name="post">The post request handler</param>
    /// <param name="checksum">The checksum request handler</param>
    public CreationFlow(
        PostRequestHandler post,
        ChecksumRequestHandler checksum
    )
    {
        this.post = post;
        this.checksum = checksum;
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
    /// Post resource creation set response headers
    /// </summary>
    public ResponseContext PostResourceCreation(ResponseContext responseContext)
    {
        // Set the following
        CommonRequestHandler.SetUploadByteOffset(responseContext);
        CommonRequestHandler.SetTusResumableHeader(responseContext);
        post.SetMaximumFileSize(responseContext);

        return responseContext;
    }
}

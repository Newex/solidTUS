using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Http;
using SolidTUS.Constants;
using SolidTUS.Contexts;
using SolidTUS.ProtocolHandlers;

namespace SolidTUS.Models;

/// <summary>
/// Request context
/// </summary>
public record RequestContext
{
    /// <summary>
    /// Instantiate a new <see cref="RequestContext"/>
    /// </summary>
    /// <param name="method">The http method</param>
    /// <param name="headers">The request headers</param>
    /// <param name="cancellationToken">The cancellation token</param>
    private RequestContext(
        string method,
        IHeaderDictionary headers,
        CancellationToken cancellationToken
    )
    {
        CancellationToken = cancellationToken;
        RequestHeaders = headers;
        ResponseHeaders = new HeaderDictionary();
        Method = method;
    }

    /// <summary>
    /// Get the http method
    /// </summary>
    public string Method { get; }

    /// <summary>
    /// Get the file Id
    /// </summary>
    public string FileID { get; set; } = string.Empty;

    /// <summary>
    /// Get the request headers
    /// </summary>
    public IHeaderDictionary RequestHeaders { get; }

    /// <summary>
    /// Get the response headers
    /// </summary>
    public IHeaderDictionary ResponseHeaders { get; }

    /// <summary>
    /// Get the upload file info
    /// </summary>
    public UploadFileInfo UploadFileInfo { get; internal set; } = new();

    /// <summary>
    /// The checksum context
    /// </summary>
    public ChecksumContext? ChecksumContext { get; init; }

    /// <summary>
    /// The partial upload file infos to be used in a final concatenation
    /// </summary>
    public IList<UploadFileInfo>? PartialFinalUploadInfos { get; internal set; }

    /// <summary>
    /// Get the cancellation token
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Create new <see cref="RequestContext"/> if request is supported by the server otherwise <see cref="HttpError"/>
    /// </summary>
    /// <param name="request">The request</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Either a success of <see cref="RequestContext"/> or an error of <see cref="HttpError"/></returns>
    public static Result<RequestContext> Create(HttpRequest request, CancellationToken cancellationToken)
    {
        var context = new RequestContext(request.Method, request.Headers, cancellationToken);
        var checkVersion = CommonRequestHandler.CheckTusVersion(context);
        return checkVersion.Map(c =>
        {
            c.ResponseHeaders.Add(TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion);
            return c;
        });
    }
}
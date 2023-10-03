using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
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
    private RequestContext(
        string method,
        IHeaderDictionary headers
    )
    {
        RequestHeaders = headers;
        Method = method;
    }

    /// <summary>
    /// Get the http method
    /// </summary>
    public string Method { get; }

    /// <summary>
    /// Get the request headers
    /// </summary>
    public IHeaderDictionary RequestHeaders { get; }

    /// <summary>
    /// The checksum context
    /// </summary>
    public ChecksumContext? ChecksumContext { get; init; }

    /// <summary>
    /// Get the partial mode
    /// </summary>
    public PartialMode PartialMode { get; internal set; }

    /// <summary>
    /// Get the parsed metadata
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; internal set; }

    /// <summary>
    /// Create new <see cref="RequestContext"/> if request is supported by the server otherwise <see cref="HttpError"/>
    /// </summary>
    /// <param name="request">The request</param>
    /// <returns>Either a success of <see cref="RequestContext"/> or an error of <see cref="HttpError"/></returns>
    public static Result<RequestContext> Create(HttpRequest request)
    {
        var context = new RequestContext(request.Method, request.Headers);
        return CommonRequestHandler.CheckTusVersion(context);
    }
}
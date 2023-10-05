using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using SolidTUS.Contexts;
using SolidTUS.ProtocolHandlers;

namespace SolidTUS.Models;

/// <summary>
/// The input and output used during a tus request response
/// </summary>
internal record TusResult
{
    /// <summary>
    /// Instantiate a new <see cref="TusResult"/>
    /// </summary>
    /// <param name="method">The http method</param>
    /// <param name="requestHeaders">The request headers</param>
    /// <param name="responseHeaders">The response headers</param>
    private TusResult(
        string method,
        IHeaderDictionary requestHeaders,
        IHeaderDictionary responseHeaders
    )
    {
        Method = method;
        RequestHeaders = requestHeaders;
        ResponseHeaders = responseHeaders;
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
    public ChecksumContext? ChecksumContext { get; internal set; }

    /// <summary>
    /// Get the partial mode
    /// </summary>
    public PartialMode PartialMode { get; internal set; }

    /// <summary>
    /// Get the parsed metadata
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; internal set; }

    /// <summary>
    /// The raw metadata string
    /// </summary>
    public string? RawMetadata { get; internal set; }

    /// <summary>
    /// The file size of current upload
    /// </summary>
    public long? FileSize { get; internal set; }

    /// <summary>
    /// Get the urls for final upload-concat.
    /// </summary>
    public string[]? Urls { get; internal set; }

    /// <summary>
    /// Get the response headers
    /// </summary>
    public IHeaderDictionary ResponseHeaders { get; }

    /// <summary>
    /// Get the related upload file info
    /// </summary>
    public UploadFileInfo? UploadFileInfo { get; internal set; }

    /// <summary>
    /// Get the created location url
    /// </summary>
    public string? LocationUrl { get; internal set; }

    /// <summary>
    /// Create new <see cref="TusResult"/> if request is supported by the server otherwise <see cref="HttpError"/>
    /// </summary>
    /// <param name="request">The request</param>
    /// <param name="response">The response</param>
    /// <returns>Either a success of <see cref="TusResult"/> or an error of <see cref="HttpError"/></returns>
    public static Result<TusResult> Create(HttpRequest request, HttpResponse response)
    {
        var context = new TusResult(request.Method, request.Headers, response.Headers);
        return CommonRequestHandler.CheckTusVersion(context);
    }

    /// <summary>
    /// The name as stored inside items
    /// </summary>
    public const string Name = "__SolidTusTusResult__";
}

using Microsoft.AspNetCore.Http;
using SolidTUS.Models;

namespace SolidTUS.Contexts;

/// <summary>
/// The response context
/// </summary>
public record ResponseContext
{
    /// <summary>
    /// Instantiate a new object <see cref="ResponseContext"/>
    /// </summary>
    /// <param name="responseHeaders">The response headers</param>
    public ResponseContext(IHeaderDictionary responseHeaders)
    {
        ResponseHeaders = responseHeaders;
    }

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
}

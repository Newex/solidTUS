using Microsoft.AspNetCore.Http;

namespace SolidTUS.Models;

/// <summary>
/// A TUS response
/// </summary>
public record TusHttpResponse
{
    /// <summary>
    /// The TUS headers
    /// </summary>
    public IHeaderDictionary Headers { get; init; } = new HeaderDictionary();

    /// <summary>
    /// The response status code
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Determines if the response was successfull
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Get or set the response message
    /// </summary>
    public string? Message { get; set; }
}

using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;

namespace SolidTUS.Models;

/// <summary>
/// Represents an http error
/// </summary>
/// <param name="StatusCode">The status code</param>
/// <param name="Headers">The headers</param>
/// <param name="Message">The optional body</param>
public record struct HttpError(int StatusCode, IHeaderDictionary Headers, string? Message = null)
{
    /// <summary>
    /// Status 400 BadRequest
    /// </summary>
    /// <param name="message">Optional message</param>
    /// <returns>An http error</returns>
    public static HttpError BadRequest(string? message = null) => new(400, new HeaderDictionary(), message);

    /// <summary>
    /// Status 403 Forbidden
    /// </summary>
    /// <param name="message">Optional message</param>
    /// <returns>An http error</returns>
    public static HttpError Forbidden(string? message = null) => new(403, new HeaderDictionary(), message);

    /// <summary>
    /// Status 404 NotFound
    /// </summary>
    /// <param name="message">Optional message</param>
    /// <returns>An http error</returns>
    public static HttpError NotFound(string? message = null) => new(404, new HeaderDictionary(), message);

    /// <summary>
    /// Status 409 Conflict
    /// </summary>
    /// <param name="message">Optional message</param>
    /// <returns>An http error</returns>
    public static HttpError Conflict(string? message = null) => new(409, new HeaderDictionary(), message);

    /// <summary>
    /// Status 410 Gone
    /// </summary>
    /// <param name="message">The optional message</param>
    /// <returns>An http error</returns>
    public static HttpError Gone(string? message = null) => new(410, new HeaderDictionary(), message);

    /// <summary>
    /// Status 412 PreconditionFailed
    /// </summary>
    /// <param name="message">Optional message</param>
    /// <returns>An http error</returns>
    public static HttpError PreconditionFailed(string? message = null) => new(412, new HeaderDictionary(), message);

    /// <summary>
    /// Status 413 Request Entity Too Large
    /// </summary>
    /// <param name="message">Optional message</param>
    /// <returns>An http error</returns>
    public static HttpError EntityTooLarge(string? message = null) => new(413, new HeaderDictionary(), message);

    /// <summary>
    /// Status 415 UnsupportedMediaType
    /// </summary>
    /// <param name="message">Optional message</param>
    /// <returns>An http error</returns>
    public static HttpError UnsupportedMediaType(string? message = null) => new(415, new HeaderDictionary(), message);

    /// <summary>
    /// Status 460 Checksum Mismatch
    /// </summary>
    /// <param name="message">Optional message</param>
    /// <returns>An http error</returns>
    public static HttpError ChecksumMismatch(string? message = null) => new(460, new HeaderDictionary(), message);

    /// <summary>
    /// Status 500 InternalServerError
    /// </summary>
    /// <param name="message">Optional message</param>
    /// <returns>An http error</returns>
    public static HttpError InternalServerError(string? message = null) => new(500, new HeaderDictionary(), message);

    /// <summary>
    /// Convert error to <see cref="Microsoft.AspNetCore.Http.IResult"/>
    /// </summary>
    /// <returns>A response result</returns>
    public readonly Microsoft.AspNetCore.Http.IResult ToResponseResult
    {
        get
        {
            return !string.IsNullOrWhiteSpace(Message)
                ? Results.Text(Message, "text/plain", statusCode: StatusCode)
                : Results.StatusCode(StatusCode);
        }
    }
}
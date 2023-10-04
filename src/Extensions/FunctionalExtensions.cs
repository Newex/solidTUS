using SolidTUS.Contexts;
using SolidTUS.Models;

namespace SolidTUS.Extensions;

/// <summary>
/// Helper extension methods
/// </summary>
internal static class FunctionalExtensions
{
    /// <summary>
    /// Get the result from an either http error or a request context as a response
    /// </summary>
    /// <param name="result">The result</param>
    /// <returns>A TUS http response</returns>
    public static HttpError? GetHttpError<T>(this Result<T> result)
    {
        return result.Match<HttpError?>(
            _ => null,
            e => e
        );
    }

    /// <summary>
    /// Return <typeparamref name="T"/> into a success <see cref="Result{R}"/>
    /// </summary>
    /// <typeparam name="T">The success result type</typeparam>
    /// <param name="input">The success value</param>
    /// <returns>A success result of <typeparamref name="T"/></returns>
    public static Result<T> Wrap<T>(this T input)
    {
        return Result<T>.Success(input);
    }

    /// <summary>
    /// Return an error <see cref="Result{R}"/>
    /// </summary>
    /// <param name="error">The error value</param>
    /// <returns>An error result</returns>
    public static Result<RequestContext> Request(this HttpError error)
    {
        return Result<RequestContext>.Error(error);
    }

    /// <summary>
    /// Return an error for the response context
    /// </summary>
    /// <param name="error">The error value</param>
    /// <returns>An error result</returns>
    public static Result<ResponseContext> Response(this HttpError error)
    {
        return Result<ResponseContext>.Error(error);
    }
}

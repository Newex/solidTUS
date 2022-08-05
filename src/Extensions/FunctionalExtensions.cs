using SolidTUS.Models;

namespace SolidTUS.Extensions;

/// <summary>
/// Helper extension methods
/// </summary>
public static class FunctionalExtensions
{
    /// <summary>
    /// Get the result from an either http error or a request context as a response
    /// </summary>
    /// <param name="result">The result</param>
    /// <param name="successStatus">The success status</param>
    /// <returns>A TUS http response</returns>
    public static TusHttpResponse GetTusHttpResponse(this Result<RequestContext> result, int successStatus = 200)
    {
        return result.Match(
            c => new TusHttpResponse
            {
                Headers = c.ResponseHeaders,
                IsSuccess = true,
                StatusCode = successStatus
            },
            e => new TusHttpResponse
            {
                IsSuccess = false,
                Headers = e.Headers,
                Message = e.Message,
                StatusCode = e.StatusCode
            }
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
    public static Result<RequestContext> Wrap(this HttpError error)
    {
        return Result<RequestContext>.Error(error);
    }
}

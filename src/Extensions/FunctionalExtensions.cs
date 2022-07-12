using LanguageExt;
using SolidTUS.Models;

namespace SolidTUS.Extensions;

/// <summary>
/// Helper extension methods
/// </summary>
public static class FunctionalExtensions
{
    /// <summary>
    /// Short hand helper
    /// </summary>
    public static class Either
    {
        /// <summary>
        /// Bake-in the left type
        /// </summary>
        /// <typeparam name="R">The right type</typeparam>
        /// <param name="right">The right value</param>
        /// <returns>An either <see cref="HttpError"/> or an <typeparamref name="R"/></returns>
        public static Either<HttpError, R> Right<R>(R right)
        {
            return Either<HttpError, R>.Right(right);
        }
    }

    /// <summary>
    /// Get the result from an either http error or a request context as a response
    /// </summary>
    /// <param name="result">The result</param>
    /// <param name="successStatus">The success status</param>
    /// <returns>A TUS http response</returns>
    public static TusHttpResponse GetTusHttpResponse(this Either<HttpError, RequestContext> result, int successStatus = 200)
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
}

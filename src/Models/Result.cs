using System;
using System.Threading.Tasks;

using SolidTUS.Extensions;

namespace SolidTUS.Models;

/// <summary>
/// A result type
/// </summary>
/// <typeparam name="R">The success result type</typeparam>
public readonly record struct Result<R>
{
    private readonly R? success;
    private readonly HttpError? error;

    private Result(R success)
    {
        if (success is null)
        {
            error = HttpError.InternalServerError();
            this.success = default;
        }
        else
        {
            this.success = success;
            error = null;
        }
    }

    private Result(HttpError error)
    {
        success = default;
        this.error = error;
    }

    /// <summary>
    /// Create an error result
    /// </summary>
    /// <param name="error">The error</param>
    /// <returns>A result with the given error</returns>
    public static Result<R> Error(HttpError error)
    {
        return new Result<R>(error);
    }

    /// <summary>
    /// Create a success result
    /// </summary>
    /// <param name="result">The result</param>
    /// <returns>A result with the given success</returns>
    public static Result<R> Success(R result)
    {
        return new Result<R>(result);
    }

    /// <summary>
    /// A flat map for the result
    /// </summary>
    /// <typeparam name="T">The transformed result type</typeparam>
    /// <param name="bind">The bind function</param>
    /// <returns>A new result</returns>
    public Result<T> Bind<T>(Func<R, Result<T>> bind)
    {
        return !error.HasValue switch
        {
            true => bind(success!),
            false => new Result<T>(error!.Value)
        };
    }

    /// <summary>
    /// Map the result from one type to another
    /// </summary>
    /// <typeparam name="T">The transformed result type</typeparam>
    /// <param name="map">The map function</param>
    /// <returns>A new result</returns>
    public Result<T> Map<T>(Func<R, T> map)
    {
        return !error.HasValue switch
        {
            true => new Result<T>(map(success!)),
            false => new Result<T>(error!.Value)
        };
    }

    /// <summary>
    /// Map the result from one type to another asynchronously
    /// </summary>
    /// <typeparam name="T">The transformed result type</typeparam>
    /// <param name="map">The map function</param>
    /// <returns>A new result type</returns>
    public async Task<Result<T>> MapAsync<T>(Func<R, Task<T>> map)
    {
        if (!error.HasValue)
        {
            var res = await map(success!);
            return res.Wrap();
        }

        return new Result<T>(error.Value);
    }

    /// <summary>
    /// Extract the result type by matching
    /// </summary>
    /// <typeparam name="T">The output type</typeparam>
    /// <param name="onSuccess">The function to call on success result</param>
    /// <param name="onError">The function to call on error result</param>
    /// <returns>A constructed <typeparamref name="T"/></returns>
    public T Match<T>(Func<R, T> onSuccess, Func<HttpError, T> onError)
    {
        return !error.HasValue switch
        {
            true => onSuccess(success!),
            false => onError(error!.Value)
        };
    }

    /// <summary>
    /// Extract the result type by matching asynchronously
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="onSuccess">Called on success</param>
    /// <param name="onError">Called on error</param>
    /// <returns>An awaitable result of <typeparamref name="T"/></returns>
    public async Task<T> MatchAsync<T>(Func<R, Task<T>> onSuccess, Func<HttpError, T> onError)
    {
        return !error.HasValue switch
        {
            true => await onSuccess(success!),
            false => onError(error.Value)
        };
    }

    // --> ASYNC equivalents
    /// <summary>
    /// Asynchronous flat map of this result
    /// </summary>
    /// <typeparam name="T">The success result type</typeparam>
    /// <param name="bindAsync">The async bind function</param>
    /// <returns>A valuetask result</returns>
    public async ValueTask<Result<T>> BindAsync<T>(Func<R, Task<Result<T>>> bindAsync)
    {
        return !error.HasValue switch
        {
            true => await bindAsync(success!),
            false => new Result<T>(error!.Value)
        };
    }

    /// <summary>
    /// Get value or default
    /// </summary>
    /// <returns>The value or default</returns>
    public R? GetValueOrDefault()
    {
        return !error.HasValue ? success : default;
    }
}

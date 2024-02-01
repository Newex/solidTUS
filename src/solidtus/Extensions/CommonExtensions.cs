using System.Collections.Generic;
using System.Threading.Tasks;
using SolidTUS.Functional.Models;
using SolidTUS.Models;

namespace SolidTUS.Extensions;

/// <summary>
/// Common functional extension methods
/// </summary>
internal static class CommonExtensions
{
    /// <summary>
    /// Convert input to a success result task.
    /// </summary>
    /// <typeparam name="T">The success type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="input">The input</param>
    /// <returns>A success result</returns>
    public static Task<Result<T, E>> ToSuccessTask<T, E>(this T input)
    where T : notnull
    {
        return Task.FromResult(Result<T, E>.Success(input));
    }

    /// <summary>
    /// Convert input to a success result value task.
    /// </summary>
    /// <typeparam name="T">The success type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="input">The input</param>
    /// <returns>A success result</returns>
    public static ValueTask<Result<T, E>> ToSuccessValueTask<T, E>(this T input)
    where T : notnull
    {
        return ValueTask.FromResult(Result<T, E>.Success(input));
    }

    /// <summary>
    /// Wrap this value into a success result.
    /// </summary>
    /// <typeparam name="T">The success value type</typeparam>
    /// <returns>A success result</returns>
    public static Result<IEnumerable<T>, HttpError> ToSuccess<T>(this IEnumerable<T> input)
    where T : notnull
    {
        return Result<IEnumerable<T>, HttpError>.Success(input);
    }

    /// <summary>
    /// Wrap this value into a success result.
    /// </summary>
    /// <typeparam name="T">The success value type</typeparam>
    /// <returns>A success result</returns>
    public static Result<T, HttpError> ToSuccess<T>(this T input)
    where T : struct
    {
        return input;
    }

    /// <summary>
    /// Wrap this value into a result
    /// </summary>
    /// <typeparam name="T">The success value type</typeparam>
    /// <param name="input">The input value</param>
    /// <returns>A result of either a success <typeparamref name="T"/> or a default <see cref="HttpError"/></returns>
    public static Result<T, HttpError> ToResult<T>(this T? input)
    where T : struct
    {
        return input is not null
            ? input.Value
            : HttpError.InternalServerError();
    }

    /// <summary>
    /// Wrap this value into a result
    /// </summary>
    /// <typeparam name="T">The success value type</typeparam>
    /// <param name="input">The input value</param>
    /// <returns>A success if not null otherwise a default error</returns>
    public static Result<T, HttpError> ToResult<T>(this T? input)
    where T : class
    {
        return input is not null
                ? input
                : HttpError.InternalServerError();
    }

    /// <summary>
    /// Convert an <see cref="IResult{T, E}"/> to a <see cref="Result{T, E}"/>.
    /// </summary>
    /// <typeparam name="T">The success type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result value</param>
    /// <returns>The result</returns>
    public static Result<T, E> ToResult<T, E>(this IResult<T, E> result)
    where T : notnull
    {
        return result switch
        {
            Result<T, E> suc => suc,
            _ => result.Extract(Result<T, E>.Success, Result<T, E>.Error),
        };

    }

    /// <summary>
    /// Covariant maybe values
    /// </summary>
    /// <typeparam name="T">The wrapped maybe value type</typeparam>
    /// <param name="input">The maybe value</param>
    /// <returns>A maybe value</returns>
    public static Maybe<IEnumerable<T>> ToMaybe<T>(this IEnumerable<T> input)
    where T : notnull
    {
        return Maybe<IEnumerable<T>>.Some(input);
    }

    /// <summary>
    /// Convert an interface maybe to an actual maybe
    /// </summary>
    /// <typeparam name="T">The wrapped maybe value type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <returns>A concrete maybe value</returns>
    public static Maybe<T> ToMaybe<T>(this IMaybe<T> maybe)
    where T : notnull
    {
        return maybe switch
        {
            Maybe<T> m => m,
            _ => maybe.Extract(Maybe<T>.Some, () => default)
        };
    }

    /// <summary>
    /// Wrap the input in a maybe if it contains a value.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <param name="input">The input value</param>
    /// <returns>A maybe value</returns>
    public static Maybe<T> ToMaybe<T>(this T? input)
    where T : struct
    {
        return input.HasValue
            ? Maybe<T>.Some(input.Value)
            : Maybe<T>.None;
    }
}

using System;
using System.Threading.Tasks;
using SolidTUS.Models.Functional;

namespace SolidTUS.Functional.Functional.ValueTaskExtensions;

/// <summary>
/// Extensions for functional <see cref="Result{T, E}"/> <see cref="ValueTask"/>
/// </summary>
internal static class ResultValueTaskExtensions
{
    /// <summary>
    /// Flat map a result.
    /// </summary>
    /// <typeparam name="T">The input success type</typeparam>
    /// <typeparam name="K">The output success type</typeparam>
    /// <typeparam name="E">The common error type</typeparam>
    /// <param name="result">The input result</param>
    /// <param name="bindValueTask">The flat map function</param>
    /// <returns>A result of either a success of <typeparamref name="K"/> or an error of <typeparamref name="E"/></returns>
    public static async ValueTask<Result<K, E>> Bind<T, K, E>(this Result<T, E> result, Func<T, ValueTask<Result<K, E>>> bindValueTask)
    where T : notnull
    where K : notnull
    {
        // right
        return result.IsSuccess
            ? await bindValueTask(result.value!)
            : Result<K, E>.Error(result.error!);
    }

    /// <summary>
    /// Flat map a result.
    /// </summary>
    /// <typeparam name="T">The input success type</typeparam>
    /// <typeparam name="K">The output success type</typeparam>
    /// <typeparam name="E">The common error type</typeparam>
    /// <param name="result">The input result</param>
    /// <param name="bindValueTask">The flat map function</param>
    /// <returns>A result of either a success of <typeparamref name="K"/> or an error of <typeparamref name="E"/></returns>
    public static async ValueTask<Result<K, E>> Bind<T, K, E>(this ValueTask<Result<T, E>> result, Func<T, Result<K, E>> bindValueTask)
    where T : notnull
    where K : notnull
    {
        // left
        var r = await result;
        return r.IsSuccess
            ? bindValueTask(r.value!)
            : Result<K, E>.Error(r.error!);
    }

    /// <summary>
    /// Flat map a result.
    /// </summary>
    /// <typeparam name="T">The input success type</typeparam>
    /// <typeparam name="K">The output success type</typeparam>
    /// <typeparam name="E">The common error type</typeparam>
    /// <param name="result">The input result</param>
    /// <param name="bindValueTask">The flat map function</param>
    /// <returns>A result of either a success of <typeparamref name="K"/> or an error of <typeparamref name="E"/></returns>
    public static async ValueTask<Result<K, E>> Bind<T, K, E>(this ValueTask<Result<T, E>> result, Func<T, ValueTask<Result<K, E>>> bindValueTask)
    where T : notnull
    where K : notnull
    {
        // both
        var r = await result;
        return r.IsSuccess
            ? await bindValueTask(r.value!)
            : Result<K, E>.Error(r.error!);
    }

    /// <summary>
    /// Map a result.
    /// </summary>
    /// <typeparam name="T">The input success type</typeparam>
    /// <typeparam name="K">The return type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="mapValueTask">The map function</param>
    /// <returns>A mapped result of either success or an error.</returns>
    public static async ValueTask<Result<K, E>> Map<T, K, E>(this Result<T, E> result, Func<T, ValueTask<K>> mapValueTask)
    where T : notnull
    where K : notnull
    {
        // right
        if (result.IsSuccess)
        {
            var value = await mapValueTask(result.value!);
            return Result<K, E>.Success(value);
        }
        else
        {
            return Result<K, E>.Error(result.error!);
        }
    }

    /// <summary>
    /// Map a result.
    /// </summary>
    /// <typeparam name="T">The input success type</typeparam>
    /// <typeparam name="K">The return type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="mapValueTask">The map function</param>
    /// <returns>A mapped result of either success or an error.</returns>
    public static async ValueTask<Result<K, E>> Map<T, K, E>(this ValueTask<Result<T, E>> result, Func<T, K> mapValueTask)
    where T : notnull
    where K : notnull
    {
        // left
        var r = await result;
        if (r.IsSuccess)
        {
            var value = mapValueTask(r.value!);
            return Result<K, E>.Success(value);
        }
        else
        {
            return Result<K, E>.Error(r.error!);
        }
    }

    /// <summary>
    /// Map a result.
    /// </summary>
    /// <typeparam name="T">The input success type</typeparam>
    /// <typeparam name="K">The return type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="mapValueTask">The map function</param>
    /// <returns>A mapped result of either success or an error.</returns>
    public static async ValueTask<Result<K, E>> Map<T, K, E>(this ValueTask<Result<T, E>> result, Func<T, ValueTask<K>> mapValueTask)
    where T : notnull
    where K : notnull
    {
        // both
        var r = await result;
        if (r.IsSuccess)
        {
            var value = await mapValueTask(r.value!);
            return Result<K, E>.Success(value);
        }
        else
        {
            return Result<K, E>.Error(r.error!);
        }
    }

    /// <summary>
    /// Extract the result of either success or error.
    /// </summary>
    /// <typeparam name="T">The input success type</typeparam>
    /// <typeparam name="R">The return type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="onSuccess">The success extraction function</param>
    /// <param name="onError">The optional error extraction function. If not given, a default <typeparamref name="R"/> value will be returned</param>
    /// <returns>Extracted result</returns>
    public static async ValueTask<R?> Match<T, R, E>(this Result<T, E> result, Func<T, ValueTask<R?>> onSuccess, Func<E, ValueTask<R?>>? onError = null)
    where T : notnull
    {
        // right
        if (result)
        {
            return await onSuccess(result.value!);
        }
        else
        {
            var match = onError ?? ((e) => default);
            return await match(result.error!);
        }
    }

    /// <summary>
    /// Extract the result of either success or error.
    /// </summary>
    /// <typeparam name="T">The input success type</typeparam>
    /// <typeparam name="R">The return type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="onSuccess">The success extraction function</param>
    /// <param name="onError">The optional error extraction function. If not given, a default <typeparamref name="R"/> value will be returned</param>
    /// <returns>Extracted result</returns>
    public static async ValueTask<R?> Match<T, R, E>(this ValueTask<Result<T, E>> result, Func<T, R?> onSuccess, Func<E, R?>? onError = null)
    where T : notnull
    {
        // left
        var r = await result;
        if (r)
        {
            return onSuccess(r.value!);
        }
        else
        {
            var match = onError ?? ((e) => default);
            return match(r.error!);
        }
    }

    /// <summary>
    /// Extract the result of either success or error.
    /// </summary>
    /// <typeparam name="T">The input success type</typeparam>
    /// <typeparam name="R">The return type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="onSuccess">The success extraction function</param>
    /// <param name="onError">The optional error extraction function. If not given, a default <typeparamref name="R"/> value will be returned</param>
    /// <returns>Extracted result</returns>
    public static async ValueTask<R?> Match<T, R, E>(this ValueTask<Result<T, E>> result, Func<T, ValueTask<R?>> onSuccess, Func<E, ValueTask<R?>>? onError = null)
    where T : notnull
    {
        // both
        var r = await result;
        if (r)
        {
            return await onSuccess(r.value!);
        }
        else
        {
            var match = onError ?? ((e) => default);
            return await match(r.error!);
        }
    }

    /// <summary>
    /// Combine 2 results by function.
    /// </summary>
    /// <typeparam name="T1">The first success result type</typeparam>
    /// <typeparam name="T2">The second success result type</typeparam>
    /// <typeparam name="R">The success return type</typeparam>
    /// <typeparam name="E">The common error type</typeparam>
    /// <param name="result">The first result</param>
    /// <param name="other">The second result</param>
    /// <param name="combineValueTask">The combination function</param>
    /// <returns>A new combined result</returns>
    public static async ValueTask<Result<R, E>> Combine<T1, T2, R, E>(this Result<T1, E> result, Result<T2, E> other, Func<T1, T2, ValueTask<R>> combineValueTask)
    where T1 : notnull
    where T2 : notnull
    where R : notnull
    {
        // right
        return await result.Bind(t1 => other.Map(async t2 => await combineValueTask(t1, t2)));
    }

    /// <summary>
    /// Combine 2 results by function.
    /// </summary>
    /// <typeparam name="T1">The first success result type</typeparam>
    /// <typeparam name="T2">The second success result type</typeparam>
    /// <typeparam name="R">The success return type</typeparam>
    /// <typeparam name="E">The common error type</typeparam>
    /// <param name="result">The first result</param>
    /// <param name="other">The second result</param>
    /// <param name="combineValueTask">The combination function</param>
    /// <returns>A new combined result</returns>
    public static async ValueTask<Result<R, E>> Combine<T1, T2, R, E>(this ValueTask<Result<T1, E>> result, Result<T2, E> other, Func<T1, T2, R> combineValueTask)
    where T1 : notnull
    where T2 : notnull
    where R : notnull
    {
        // left
        return await result.Bind(t1 => other.Map(t2 => combineValueTask(t1, t2)));
    }

    /// <summary>
    /// Combine 2 results by function.
    /// </summary>
    /// <typeparam name="T1">The first success result type</typeparam>
    /// <typeparam name="T2">The second success result type</typeparam>
    /// <typeparam name="R">The success return type</typeparam>
    /// <typeparam name="E">The common error type</typeparam>
    /// <param name="result">The first result</param>
    /// <param name="other">The second result</param>
    /// <param name="combineValueTask">The combination function</param>
    /// <returns>A new combined result</returns>
    public static async ValueTask<Result<R, E>> Combine<T1, T2, R, E>(this ValueTask<Result<T1, E>> result, Result<T2, E> other, Func<T1, T2, ValueTask<R>> combineValueTask)
    where T1 : notnull
    where T2 : notnull
    where R : notnull
    {
        // both
        return await result.Bind(t1 => other.Map(async t2 => await combineValueTask(t1, t2)));
    }

    /// <summary>
    /// Combine 2 results by function.
    /// </summary>
    /// <typeparam name="T1">The first success result type</typeparam>
    /// <typeparam name="T2">The second success result type</typeparam>
    /// <typeparam name="E">The common error type</typeparam>
    /// <param name="result">The first result</param>
    /// <param name="other">The second result</param>
    /// <returns>A new combined result</returns>
    public static async ValueTask<Result<(T1, T2), E>> Combine<T1, T2, E>(this Result<T1, E> result, ValueTask<Result<T2, E>> other)
    where T1 : notnull
    where T2 : notnull
    {
        // right
        return await result.Bind(async t1 => await other.Map(t2 => (t1, t2)));
    }

    /// <summary>
    /// Combine 2 results by function.
    /// </summary>
    /// <typeparam name="T1">The first success result type</typeparam>
    /// <typeparam name="T2">The second success result type</typeparam>
    /// <typeparam name="E">The common error type</typeparam>
    /// <param name="result">The first result</param>
    /// <param name="other">The second result</param>
    /// <returns>A new combined result</returns>
    public static async ValueTask<Result<(T1, T2), E>> Combine<T1, T2, E>(this ValueTask<Result<T1, E>> result, Result<T2, E> other)
    where T1 : notnull
    where T2 : notnull
    {
        // left
        return await result.Bind(t1 => other.Map(t2 => (t1, t2)));
    }

    /// <summary>
    /// Combine 2 results by function.
    /// </summary>
    /// <typeparam name="T1">The first success result type</typeparam>
    /// <typeparam name="T2">The second success result type</typeparam>
    /// <typeparam name="E">The common error type</typeparam>
    /// <param name="result">The first result</param>
    /// <param name="other">The second result</param>
    /// <returns>A new combined result</returns>
    public static async ValueTask<Result<(T1, T2), E>> Combine<T1, T2, E>(this ValueTask<Result<T1, E>> result, ValueTask<Result<T2, E>> other)
    where T1 : notnull
    where T2 : notnull
    {
        // both
        return await result.Bind(async t1 => await other.Map(t2 => (t1, t2)));
    }

    /// <summary>
    /// Compensate the result such that if it is an error, an opportunity to correct the error will be called by the compensation function.
    /// </summary>
    /// <typeparam name="T">The success type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="compensateValueTask">The compensation function</param>
    /// <returns>A potentially new result</returns>
    public static async ValueTask<Result<T, E>> Compensate<T, E>(this Result<T, E> result, Func<E, ValueTask<Result<T, E>>> compensateValueTask)
    where T : notnull
    {
        // right
        return result
            ? result
            : await compensateValueTask(result.error!);
    }

    /// <summary>
    /// Compensate the result such that if it is an error, an opportunity to correct the error will be called by the compensation function.
    /// </summary>
    /// <typeparam name="T">The success type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="compensateValueTask">The compensation function</param>
    /// <returns>A potentially new result</returns>
    public static async ValueTask<Result<T, E>> Compensate<T, E>(this ValueTask<Result<T, E>> result, Func<E, Result<T, E>> compensateValueTask)
    where T : notnull
    {
        // left
        var r = await result;
        return r
            ? r
            : compensateValueTask(r.error!);
    }

    /// <summary>
    /// Compensate the result such that if it is an error, an opportunity to correct the error will be called by the compensation function.
    /// </summary>
    /// <typeparam name="T">The success type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="compensateValueTask">The compensation function</param>
    /// <returns>A potentially new result</returns>
    public static async ValueTask<Result<T, E>> Compensate<T, E>(this Task<Result<T, E>> result, Func<E, ValueTask<Result<T, E>>> compensateValueTask)
    where T : notnull
    {
        // both
        var r = await result;
        return r
            ? r
            : await compensateValueTask(r.error!);
    }

    /// <summary>
    /// Execute action on success result.
    /// </summary>
    /// <typeparam name="T">The success type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="action">The action to execute</param>
    /// <returns>The original result</returns>
    public static async ValueTask<Result<T, E>> Peek<T, E>(this Result<T, E> result, Func<T, ValueTask> action)
    where T : notnull
    {
        if (result)
        {
            await action(result.value!);
        }

        return result;
    }

    /// <summary>
    /// Execute action on error result.
    /// </summary>
    /// <typeparam name="T">The success type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="action">The action to execute</param>
    /// <returns>The original result</returns>
    public static async ValueTask<Result<T, E>> PeekOnError<T, E>(this Result<T, E> result, Func<E, ValueTask> action)
    where T : notnull
    {
        if (!result)
        {
            await action(result.error!);
        }

        return result;
    }
}

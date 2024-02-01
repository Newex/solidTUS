using System;
using System.Threading.Tasks;

namespace SolidTUS.Functional.Models;

/// <summary>
/// Result extensions
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Flat map a result.
    /// </summary>
    /// <typeparam name="T">The input success type</typeparam>
    /// <typeparam name="K">The output success type</typeparam>
    /// <typeparam name="E">The common error type</typeparam>
    /// <param name="result">The input result</param>
    /// <param name="bind">The flat map function</param>
    /// <returns>A result of either a success of <typeparamref name="K"/> or an error of <typeparamref name="E"/></returns>
    public static Result<K, E> Bind<T, K, E>(this Result<T, E> result, Func<T, Result<K, E>> bind)
    where T : notnull
    where K : notnull
    {
        // Normal
        return result.IsSuccess
            ? bind(result.value!)
            : Result<K, E>.Error(result.error!);
    }

    /// <summary>
    /// Flat map a result.
    /// </summary>
    /// <typeparam name="T">The input success type</typeparam>
    /// <typeparam name="K">The output success type</typeparam>
    /// <typeparam name="E">The common error type</typeparam>
    /// <param name="result">The input result</param>
    /// <param name="bindTask">The flat map function</param>
    /// <returns>A result of either a success of <typeparamref name="K"/> or an error of <typeparamref name="E"/></returns>
    public static async Task<Result<K, E>> Bind<T, K, E>(this Result<T, E> result, Func<T, Task<Result<K, E>>> bindTask)
    where T : notnull
    where K : notnull
    {
        // Task right
        return result.IsSuccess
            ? await bindTask(result.value!).ConfigureAwait(false)
            : Result<K, E>.Error(result.error!);
    }

    /// <summary>
    /// Flat map a result.
    /// </summary>
    /// <typeparam name="T">The input success type</typeparam>
    /// <typeparam name="K">The output success type</typeparam>
    /// <typeparam name="E">The common error type</typeparam>
    /// <param name="result">The input result</param>
    /// <param name="bindTask">The flat map function</param>
    /// <returns>A result of either a success of <typeparamref name="K"/> or an error of <typeparamref name="E"/></returns>
    public static async Task<Result<K, E>> Bind<T, K, E>(this Task<Result<T, E>> result, Func<T, Result<K, E>> bindTask)
    where T : notnull
    where K : notnull
    {
        // Task left
        var r = await result.ConfigureAwait(false);
        return r.IsSuccess
            ? bindTask(r.value!)
            : Result<K, E>.Error(r.error!);
    }

    /// <summary>
    /// Flat map a result.
    /// </summary>
    /// <typeparam name="T">The input success type</typeparam>
    /// <typeparam name="K">The output success type</typeparam>
    /// <typeparam name="E">The common error type</typeparam>
    /// <param name="result">The input result</param>
    /// <param name="bindTask">The flat map function</param>
    /// <returns>A result of either a success of <typeparamref name="K"/> or an error of <typeparamref name="E"/></returns>
    public static async Task<Result<K, E>> Bind<T, K, E>(this Task<Result<T, E>> result, Func<T, Task<Result<K, E>>> bindTask)
    where T : notnull
    where K : notnull
    {
        // Task both
        var r = await result.ConfigureAwait(false);
        return r.IsSuccess
            ? await bindTask(r.value!).ConfigureAwait(false)
            : Result<K, E>.Error(r.error!);
    }

    /// <summary>
    /// Compensate the result such that if it is an error, an opportunity to correct the error will be called by the compensation function.
    /// </summary>
    /// <typeparam name="T">The success type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="compensate">The compensation function</param>
    /// <returns>A potentially new result</returns>
    public static Result<T, E> BindOnError<T, E>(this Result<T, E> result, Func<E, Result<T, E>> compensate)
    where T : notnull
    {
        return result
            ? result
            : compensate(result.error!);
    }

    /// <summary>
    /// Compensate the result such that if it is an error, an opportunity to correct the error will be called by the compensation function.
    /// </summary>
    /// <typeparam name="T">The success type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="compensate">The compensation function</param>
    /// <returns>A potentially new result</returns>
    public static async Task<Result<T, E>> BindOnError<T, E>(this Result<T, E> result, Func<E, Task<Result<T, E>>> compensate)
    where T : notnull
    {
        // right
        return result
            ? result
            : await compensate(result.error!).ConfigureAwait(false);
    }

    /// <summary>
    /// Compensate the result such that if it is an error, an opportunity to correct the error will be called by the compensation function.
    /// </summary>
    /// <typeparam name="T">The success type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="compensate">The compensation function</param>
    /// <returns>A potentially new result</returns>
    public static async Task<Result<T, E>> BindOnError<T, E>(this Task<Result<T, E>> result, Func<E, Result<T, E>> compensate)
    where T : notnull
    {
        // left
        var r = await result.ConfigureAwait(false);
        return r
            ? r
            : compensate(r.error!);
    }

    /// <summary>
    /// Compensate the result such that if it is an error, an opportunity to correct the error will be called by the compensation function.
    /// </summary>
    /// <typeparam name="T">The success type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="compensate">The compensation function</param>
    /// <returns>A potentially new result</returns>
    public static async Task<Result<T, E>> BindOnError<T, E>(this Task<Result<T, E>> result, Func<E, Task<Result<T, E>>> compensate)
    where T : notnull
    {
        // both
        var r = await result.ConfigureAwait(false);
        return r
            ? r
            : await compensate(r.error!).ConfigureAwait(false);
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
    public static R? Match<T, R, E>(this Result<T, E> result, Func<T, R?> onSuccess, Func<E, R?>? onError = null)
    where T : notnull
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.value!);
        }
        else
        {
            var match = onError ?? ((e) => default);
            return match(result.error!);
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
    public static async Task<R?> Match<T, R, E>(this Result<T, E> result, Func<T, Task<R?>> onSuccess, Func<E, Task<R?>>? onError = null)
    where T : notnull
    {
        // right
        if (result.IsSuccess)
        {
            return await onSuccess(result.value!).ConfigureAwait(false);
        }
        else
        {
            var match = onError ?? ((e) => Task.FromResult<R?>(default));
            return await match(result.error!).ConfigureAwait(false);
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
    public static async Task<R?> Match<T, R, E>(this Task<Result<T, E>> result, Func<T, R?> onSuccess, Func<E, R?>? onError = null)
    where T : notnull
    {
        // left
        var r = await result.ConfigureAwait(false);
        if (r.IsSuccess)
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
    public static async Task<R?> Match<T, R, E>(this Task<Result<T, E>> result, Func<T, Task<R?>> onSuccess, Func<E, Task<R?>>? onError = null)
    where T : notnull
    {
        // both
        var r = await result.ConfigureAwait(false);
        if (r.IsSuccess)
        {
            return await onSuccess(r.value!).ConfigureAwait(false);
        }
        else
        {
            var match = onError ?? ((e) => Task.FromResult<R?>(default));
            return await match(r.error!).ConfigureAwait(false);
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
    public static R MatchValue<T, R, E>(this Result<T, E> result, Func<T, R> onSuccess, Func<E, R> onError)
    where T : notnull
    where R : notnull
    where E : notnull
    {
        return result.IsSuccess
            ? onSuccess(result.value!)
            : onError(result.error!);
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
    public static async Task<R> MatchValue<T, R, E>(this Result<T, E> result, Func<T, Task<R>> onSuccess, Func<E, Task<R>> onError)
    where T : notnull
    where R : notnull
    where E : notnull
    {
        // right
        return result.IsSuccess
            ? await onSuccess(result.value!).ConfigureAwait(false)
            : await onError(result.error!).ConfigureAwait(false);
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
    public static async Task<R> MatchValue<T, R, E>(this Task<Result<T, E>> result, Func<T, R> onSuccess, Func<E, R> onError)
    where T : notnull
    where R : notnull
    where E : notnull
    {
        // left
        var r = await result.ConfigureAwait(false);
        return r.IsSuccess
            ? onSuccess(r.value!)
            : onError(r.error!);
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
    public static async Task<R> MatchValue<T, R, E>(this Task<Result<T, E>> result, Func<T, Task<R>> onSuccess, Func<E, Task<R>> onError)
    where T : notnull
    where R : notnull
    where E : notnull
    {
        // both
        var r = await result.ConfigureAwait(false);
        return r.IsSuccess
            ? await onSuccess(r.value!).ConfigureAwait(false)
            : await onError(r.error!).ConfigureAwait(false);
    }

    /// <summary>
    /// Map a result.
    /// </summary>
    /// <typeparam name="T">The input success type</typeparam>
    /// <typeparam name="K">The return type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="map">The map function</param>
    /// <returns>A mapped result of either success or an error.</returns>
    public static Result<K, E> Map<T, K, E>(this Result<T, E> result, Func<T, K> map)
    where T : notnull
    where K : notnull
    {
        return result.IsSuccess
            ? map(result.value!)
            : Result<K, E>.Error(result.error!);
    }

    /// <summary>
    /// Map a result.
    /// </summary>
    /// <typeparam name="T">The input success type</typeparam>
    /// <typeparam name="K">The return type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="map">The map function</param>
    /// <returns>A mapped result of either success or an error.</returns>
    public static async Task<Result<K, E>> Map<T, K, E>(this Result<T, E> result, Func<T, Task<K>> map)
    where T : notnull
    where K : notnull
    {
        // right
        if (result.IsSuccess)
        {
            var value = await map(result.value!).ConfigureAwait(false);
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
    /// <param name="map">The map function</param>
    /// <returns>A mapped result of either success or an error.</returns>
    public static async Task<Result<K, E>> Map<T, K, E>(this Task<Result<T, E>> result, Func<T, K> map)
    where T : notnull
    where K : notnull
    {
        // left
        var r = await result.ConfigureAwait(false);
        if (r.IsSuccess)
        {
            var value = map(r.value!);
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
    /// <param name="map">The map function</param>
    /// <returns>A mapped result of either success or an error.</returns>
    public static async Task<Result<K, E>> Map<T, K, E>(this Task<Result<T, E>> result, Func<T, Task<K>> map)
    where T : notnull
    where K : notnull
    {
        // both
        var r = await result.ConfigureAwait(false);
        if (r.IsSuccess)
        {
            var value = await map(r.value!).ConfigureAwait(false);
            return Result<K, E>.Success(value);
        }
        else
        {
            return Result<K, E>.Error(r.error!);
        }
    }

    /// <summary>
    /// Map the error result, if the result is an error otherwise keep the success value.
    /// </summary>
    /// <typeparam name="T">The success type value</typeparam>
    /// <typeparam name="E1">The input error type</typeparam>
    /// <typeparam name="E2">The mapped error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="map">The map error function</param>
    /// <returns>A result</returns>
    public static Result<T, E2> MapOnError<T, E1, E2>(this Result<T, E1> result, Func<E1, E2> map)
    where T : notnull
    {
        return result.IsSuccess
            ? result.value!
            : map(result.error!);
    }

    /// <summary>
    /// Map the error result, if the result is an error otherwise keep the success value.
    /// </summary>
    /// <typeparam name="T">The success type value</typeparam>
    /// <typeparam name="E1">The input error type</typeparam>
    /// <typeparam name="E2">The mapped error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="map">The map error function</param>
    /// <returns>A result</returns>
    public static async Task<Result<T, E2>> MapOnError<T, E1, E2>(this Result<T, E1> result, Func<E1, Task<E2>> map)
    where T : notnull
    {
        // right
        return result.IsSuccess
            ? result.value!
            : await map(result.error!).ConfigureAwait(false);
    }

    /// <summary>
    /// Map the error result, if the result is an error otherwise keep the success value.
    /// </summary>
    /// <typeparam name="T">The success type value</typeparam>
    /// <typeparam name="E1">The input error type</typeparam>
    /// <typeparam name="E2">The mapped error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="map">The map error function</param>
    /// <returns>A result</returns>
    public static async Task<Result<T, E2>> MapOnError<T, E1, E2>(this Task<Result<T, E1>> result, Func<E1, E2> map)
    where T : notnull
    {
        // left
        var r = await result.ConfigureAwait(false);
        return r.IsSuccess
            ? r.value!
            : map(r.error!);
    }

    /// <summary>
    /// Map the error result, if the result is an error otherwise keep the success value.
    /// </summary>
    /// <typeparam name="T">The success type value</typeparam>
    /// <typeparam name="E1">The input error type</typeparam>
    /// <typeparam name="E2">The mapped error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="map">The map error function</param>
    /// <returns>A result</returns>
    public static async Task<Result<T, E2>> MapOnError<T, E1, E2>(this Task<Result<T, E1>> result, Func<E1, Task<E2>> map)
    where T : notnull
    {
        // both
        var r = await result.ConfigureAwait(false);
        return r.IsSuccess
            ? r.value!
            : await map(r.error!).ConfigureAwait(false);
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
    /// <param name="combine">The combination function</param>
    /// <returns>A new combined result</returns>
    public static Result<R, E> Combine<T1, T2, R, E>(this Result<T1, E> result, Result<T2, E> other, Func<T1, T2, R> combine)
    where T1 : notnull
    where T2 : notnull
    where R : notnull
    {
        return result.Bind(t1 => other.Map(t2 => combine(t1, t2)));
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
    /// <param name="combine">The combination function</param>
    /// <returns>A new combined result</returns>
    public static async Task<Result<R, E>> Combine<T1, T2, R, E>(this Result<T1, E> result, Result<T2, E> other, Func<T1, T2, Task<R>> combine)
    where T1 : notnull
    where T2 : notnull
    where R : notnull
    {
        // right
        return await result.Bind(t1 => other.Map(async t2 => await combine(t1, t2).ConfigureAwait(false))).ConfigureAwait(false);
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
    /// <param name="combine">The combination function</param>
    /// <returns>A new combined result</returns>
    public static async Task<Result<R, E>> Combine<T1, T2, R, E>(this Task<Result<T1, E>> result, Result<T2, E> other, Func<T1, T2, R> combine)
    where T1 : notnull
    where T2 : notnull
    where R : notnull
    {
        // left
        return await result.Bind(t1 => other.Map(t2 => combine(t1, t2))).ConfigureAwait(false);
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
    /// <param name="combine">The combination function</param>
    /// <returns>A new combined result</returns>
    public static async Task<Result<R, E>> Combine<T1, T2, R, E>(this Task<Result<T1, E>> result, Result<T2, E> other, Func<T1, T2, Task<R>> combine)
    where T1 : notnull
    where T2 : notnull
    where R : notnull
    {
        // both
        return await result.Bind(t1 => other.Map(async t2 => await combine(t1, t2).ConfigureAwait(false))).ConfigureAwait(false);
    }

    /// <summary>
    /// Combine 2 results into a tuple.
    /// </summary>
    /// <typeparam name="T1">The first success result type</typeparam>
    /// <typeparam name="T2">The second success result type</typeparam>
    /// <typeparam name="E">The common error type</typeparam>
    /// <param name="result">The first result</param>
    /// <param name="other">The second result</param>
    /// <returns>A new combined result</returns>
    public static Result<(T1, T2), E> Combine<T1, T2, E>(this Result<T1, E> result, Result<T2, E> other)
    where T1 : notnull
    where T2 : notnull
    {
        return result.Bind(t1 => other.Map(t2 => (t1, t2)));
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
    public static async Task<Result<(T1, T2), E>> Combine<T1, T2, E>(this Task<Result<T1, E>> result, Result<T2, E> other)
    where T1 : notnull
    where T2 : notnull
    {
        // left
        return await result.Bind(t1 => other.Map(t2 => (t1, t2))).ConfigureAwait(false);
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
    public static async Task<Result<(T1, T2), E>> Combine<T1, T2, E>(this Result<T1, E> result, Task<Result<T2, E>> other)
    where T1 : notnull
    where T2 : notnull
    {
        // right
        return await result.Bind(async t1 => await other.Map(t2 => (t1, t2)).ConfigureAwait(false)).ConfigureAwait(false);
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
    public static async Task<Result<(T1, T2), E>> Combine<T1, T2, E>(this Task<Result<T1, E>> result, Task<Result<T2, E>> other)
    where T1 : notnull
    where T2 : notnull
    {
        // both
        return await result.Bind(async t1 => await other.Map(t2 => (t1, t2)).ConfigureAwait(false)).ConfigureAwait(false);
    }

    /// <summary>
    /// Execute action on success result.
    /// </summary>
    /// <typeparam name="T">The success type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="action">The action to execute</param>
    /// <returns>The original result</returns>
    public static Result<T, E> Peek<T, E>(this Result<T, E> result, Action<T> action)
    where T : notnull
    {
        if (result)
        {
            action(result.value!);
        }

        return result;
    }

    /// <summary>
    /// Execute action on success result.
    /// </summary>
    /// <typeparam name="T">The success type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="action">The action to execute</param>
    /// <returns>The original result</returns>
    public static async Task<Result<T, E>> Peek<T, E>(this Result<T, E> result, Func<T, Task> action)
    where T : notnull
    {
        // right
        if (result)
        {
            await action(result.value!).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Execute action on success result.
    /// </summary>
    /// <typeparam name="T">The success type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="action">The action to execute</param>
    /// <returns>The original result</returns>
    public static async Task<Result<T, E>> Peek<T, E>(this Task<Result<T, E>> result, Action<T> action)
    where T : notnull
    {
        // left
        var r = await result.ConfigureAwait(false);
        if (r)
        {
            action(r.value!);
        }

        return r;
    }

    /// <summary>
    /// Execute action on success result.
    /// </summary>
    /// <typeparam name="T">The success type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="action">The action to execute</param>
    /// <returns>The original result</returns>
    public static async Task<Result<T, E>> Peek<T, E>(this Task<Result<T, E>> result, Func<T, Task> action)
    where T : notnull
    {
        // both
        var r = await result.ConfigureAwait(false);
        if (r)
        {
            await action(r.value!).ConfigureAwait(false);
        }

        return r;
    }

    /// <summary>
    /// Execute action on error result.
    /// </summary>
    /// <typeparam name="T">The success type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="action">The action to execute</param>
    /// <returns>The original result</returns>
    public static Result<T, E> PeekOnError<T, E>(this Result<T, E> result, Action<E> action)
    where T : notnull
    {
        if (!result)
        {
            action(result.error!);
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
    public static async Task<Result<T, E>> PeekOnError<T, E>(this Result<T, E> result, Func<E, Task> action)
    where T : notnull
    {
        // right
        if (!result)
        {
            await action(result.error!).ConfigureAwait(false);
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
    public static async Task<Result<T, E>> PeekOnError<T, E>(this Task<Result<T, E>> result, Action<E> action)
    where T : notnull
    {
        // left
        var r = await result.ConfigureAwait(false);
        if (!r)
        {
            action(r.error!);
        }

        return r;
    }

    /// <summary>
    /// Execute action on error result.
    /// </summary>
    /// <typeparam name="T">The success type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="action">The action to execute</param>
    /// <returns>The original result</returns>
    public static async Task<Result<T, E>> PeekOnError<T, E>(this Task<Result<T, E>> result, Func<E, Task> action)
    where T : notnull
    {
        // both
        var r = await result.ConfigureAwait(false);
        if (!r)
        {
            await action(r.error!).ConfigureAwait(false);
        }

        return r;
    }

    /// <summary>
    /// Deconstruct the result into a tuple.
    /// </summary>
    /// <typeparam name="T">The success type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="success">The success value.</param>
    /// <param name="error">The error value.</param>
    public static void Deconstruct<T, E>(this Result<T, E> result, out T? success, out E? error)
    where T : struct
    {
        // Reason for duplicate code is to make structs nullable
        success = result.IsSuccess ? result.value : null;
        error = result.error;
    }

    /// <summary>
    /// Deconstruct the result into a tuple.
    /// </summary>
    /// <typeparam name="T">The success type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="success">The success value.</param>
    /// <param name="error">The error value.</param>
    public static void Deconstruct<T, E>(this Result<T, E> result, out T? success, out E? error)
    where T : class
    {
        // Reason for duplicate code is to make structs nullable
        success = result.IsSuccess ? result.value : null;
        error = result.error;
    }

    /// <summary>
    /// Deconstruct the result into a triple.
    /// </summary>
    /// <typeparam name="T">The success type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="isSuccess">The success indicator. True if success otherwise false.</param>
    /// <param name="success">The success value.</param>
    /// <param name="error">The error value.</param>
    public static void Deconstruct<T, E>(this Result<T, E> result, out bool isSuccess, out T? success, out E? error)
    where T : struct
    {
        // Reason for duplicate code is to make structs nullable
        isSuccess = result.IsSuccess;
        success = isSuccess ? result.value : null;
        error = result.error;
    }

    /// <summary>
    /// Deconstruct the result into a triple.
    /// </summary>
    /// <typeparam name="T">The success type</typeparam>
    /// <typeparam name="E">The error type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="isSuccess">The success indicator. True if success otherwise false.</param>
    /// <param name="success">The success value.</param>
    /// <param name="error">The error value.</param>
    public static void Deconstruct<T, E>(this Result<T, E> result, out bool isSuccess, out T? success, out E? error)
    where T : class
    {
        // Reason for duplicate code is to make structs nullable
        isSuccess = result.IsSuccess;
        success = result.value;
        error = result.error;
    }
}

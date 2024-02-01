using System;
using System.Threading.Tasks;

namespace SolidTUS.Functional.Models;

/// <summary>
/// Maybe extensions
/// </summary>
internal static class MaybeExtensions
{
    /// <summary>
    /// Bind maybe using a flat map.
    /// </summary>
    /// <typeparam name="T">The input type</typeparam>
    /// <typeparam name="K">The output type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="bind">The flat map function</param>
    /// <returns>A maybe value</returns>
    public static Maybe<K> Bind<T, K>(this Maybe<T> maybe, Func<T, Maybe<K>> bind)
    where T : notnull
    where K : notnull
    {
        // normal
        return maybe.hasValue
            ? bind(maybe.value!)
            : Maybe<K>.None;
    }

    /// <summary>
    /// Bind maybe using a flat map.
    /// </summary>
    /// <typeparam name="T">The input type</typeparam>
    /// <typeparam name="K">The output type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="bind">The flat map function</param>
    /// <returns>A maybe value</returns>
    public static async Task<Maybe<K>> Bind<T, K>(this Maybe<T> maybe, Func<T, Task<Maybe<K>>> bind)
    where T : notnull
    where K : notnull
    {
        // right
        return maybe.hasValue
            ? await bind(maybe.value!).ConfigureAwait(false)
            : Maybe<K>.None;
    }

    /// <summary>
    /// Bind maybe using a flat map.
    /// </summary>
    /// <typeparam name="T">The input type</typeparam>
    /// <typeparam name="K">The output type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="bind">The flat map function</param>
    /// <returns>A maybe value</returns>
    public static async Task<Maybe<K>> Bind<T, K>(this Task<Maybe<T>> maybe, Func<T, Maybe<K>> bind)
    where T : notnull
    where K : notnull
    {
        // left
        var m = await maybe.ConfigureAwait(false);
        return m.hasValue
            ? bind(m.value!)
            : Maybe<K>.None;
    }

    /// <summary>
    /// Bind maybe using a flat map.
    /// </summary>
    /// <typeparam name="T">The input type</typeparam>
    /// <typeparam name="K">The output type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="bind">The flat map function</param>
    /// <returns>A maybe value</returns>
    public static async Task<Maybe<K>> Bind<T, K>(this Task<Maybe<T>> maybe, Func<T, Task<Maybe<K>>> bind)
    where T : notnull
    where K : notnull
    {
        // both
        var m = await maybe.ConfigureAwait(false);
        return m.hasValue
            ? await bind(m.value!).ConfigureAwait(false)
            : Maybe<K>.None;
    }

    /// <summary>
    /// Bind maybe using a flat map when maybe is a none.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe value type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="bind">The flat map function executed on error</param>
    /// <returns>A maybe value</returns>
    public static Maybe<T> BindOnError<T>(this Maybe<T> maybe, Func<Maybe<T>> bind)
    where T : notnull
    {
        // normal
        return maybe.HasValue
            ? maybe
            : bind();
    }

    /// <summary>
    /// Bind maybe using a flat map when maybe is a none.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe value type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="bind">The flat map function executed on error</param>
    /// <returns>A maybe value</returns>
    public static async Task<Maybe<T>> BindOnError<T>(this Task<Maybe<T>> maybe, Func<Maybe<T>> bind)
    where T : notnull
    {
        // left
        var m = await maybe.ConfigureAwait(false);
        return m.HasValue
            ? m
            : bind();
    }

    /// <summary>
    /// Bind maybe using a flat map when maybe is a none.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe value type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="bind">The flat map function executed on error</param>
    /// <returns>A maybe value</returns>
    public static async Task<Maybe<T>> BindOnError<T>(this Maybe<T> maybe, Func<Task<Maybe<T>>> bind)
    where T : notnull
    {
        // right
        return maybe.HasValue
            ? maybe
            : await bind().ConfigureAwait(false);
    }

    /// <summary>
    /// Bind maybe using a flat map when maybe is a none.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe value type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="bind">The flat map function executed on error</param>
    /// <returns>A maybe value</returns>
    public static async Task<Maybe<T>> BindOnError<T>(this Task<Maybe<T>> maybe, Func<Task<Maybe<T>>> bind)
    where T : notnull
    {
        // both
        var m = await maybe.ConfigureAwait(false);
        return m.HasValue
            ? m
            : await bind().ConfigureAwait(false);
    }

    /// <summary>
    /// Extract the result using the matching function.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <typeparam name="R">The result type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="match">The match function</param>
    /// <param name="default">The optional default return value if this contains none</param>
    /// <returns>An extracted matched value or default</returns>
    public static R? Match<T, R>(this Maybe<T> maybe, Func<T, R> match, R? @default = default)
    where T : notnull
    {
        return maybe.hasValue
            ? match(maybe.value!)
            : @default;
    }

    /// <summary>
    /// Extract the result using the matching function.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <typeparam name="R">The result type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="match">The match function</param>
    /// <param name="default">The optional default return value if this contains none</param>
    /// <returns>An extracted matched value or default</returns>
    public static async Task<R?> Match<T, R>(this Maybe<T> maybe, Func<T, Task<R>> match, R? @default = default)
    where T : notnull
    {
        // right
        return maybe.hasValue
            ? await match(maybe.value!).ConfigureAwait(false)
            : @default;
    }

    /// <summary>
    /// Extract the result using the matching function.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <typeparam name="R">The result type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="match">The match function</param>
    /// <param name="default">The optional default return value if this contains none</param>
    /// <returns>An extracted matched value or default</returns>
    public static async Task<R?> Match<T, R>(this Task<Maybe<T>> maybe, Func<T, R> match, R? @default = default)
    where T : notnull
    {
        // left
        var m = await maybe.ConfigureAwait(false);
        return m.hasValue
            ? match(m.value!)
            : @default;
    }

    /// <summary>
    /// Extract the result using the matching function.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <typeparam name="R">The result type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="match">The match function</param>
    /// <param name="default">The optional default return value if this contains none</param>
    /// <returns>An extracted matched value or default</returns>
    public static async Task<R?> Match<T, R>(this Task<Maybe<T>> maybe, Func<T, Task<R>> match, R? @default = default)
    where T : notnull
    {
        // both
        var m = await maybe.ConfigureAwait(false);
        return m.hasValue
            ? await match(m.value!).ConfigureAwait(false)
            : @default;
    }

    /// <summary>
    /// Extract the result using the matching function.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <typeparam name="R">The result type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="match">The match function</param>
    /// <returns>An extracted matched value or default</returns>
    public static R MatchValue<T, R>(this Maybe<T> maybe, Func<T, R> match)
    where T : notnull
    where R : struct
    {
        return maybe.HasValue
            ? match(maybe.value!)
            : default;
    }

    /// <summary>
    /// Extract the result using the matching function.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <typeparam name="R">The result type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="match">The match function</param>
    /// <returns>An extracted matched value or default</returns>
    public static async Task<R> MatchValue<T, R>(this Maybe<T> maybe, Func<T, Task<R>> match)
    where T : notnull
    where R : struct
    {
        // right
        return maybe.hasValue
            ? await match(maybe.value!).ConfigureAwait(false)
            : default;
    }

    /// <summary>
    /// Extract the result using the matching function.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <typeparam name="R">The result type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="match">The match function</param>
    /// <returns>An extracted matched value or default</returns>
    public static async Task<R> MatchValue<T, R>(this Task<Maybe<T>> maybe, Func<T, R> match)
    where T : notnull
    where R : struct
    {
        // left
        var m = await maybe.ConfigureAwait(false);
        return m.hasValue
            ? match(m.value!)
            : default;
    }

    /// <summary>
    /// Extract the result using the matching function.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <typeparam name="R">The result type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="match">The match function</param>
    /// <returns>An extracted matched value or default</returns>
    public static async Task<R> MatchValue<T, R>(this Task<Maybe<T>> maybe, Func<T, Task<R>> match)
    where T : notnull
    where R : struct
    {
        // both
        var m = await maybe.ConfigureAwait(false);
        return m.hasValue
            ? await match(m.value!).ConfigureAwait(false)
            : default;
    }

    /// <summary>
    /// Extract the result using the matching function.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <typeparam name="R">The result type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="match">The match function</param>
    /// <param name="default">The default return value if none</param>
    /// <returns>An extracted matched value or default</returns>
    public static R MatchValue<T, R>(this Maybe<T> maybe, Func<T, R> match, R @default)
    where T : notnull
    where R : notnull
    {
        return maybe.HasValue
            ? match(maybe.value!)
            : @default;
    }

    /// <summary>
    /// Extract the result using the matching function.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <typeparam name="R">The result type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="match">The match function</param>
    /// <param name="default">The default return value if none</param>
    /// <returns>An extracted matched value or default</returns>
    public static async Task<R> MatchValue<T, R>(this Maybe<T> maybe, Func<T, Task<R>> match, R @default)
    where T : notnull
    where R : notnull
    {
        // right
        return maybe.hasValue
            ? await match(maybe.value!).ConfigureAwait(false)
            : @default;
    }

    /// <summary>
    /// Extract the result using the matching function.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <typeparam name="R">The result type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="match">The match function</param>
    /// <param name="default">The default return value if none</param>
    /// <returns>An extracted matched value or default</returns>
    public static async Task<R> MatchValue<T, R>(this Task<Maybe<T>> maybe, Func<T, R> match, R @default)
    where T : notnull
    where R : notnull
    {
        // left
        var m = await maybe.ConfigureAwait(false);
        return m.hasValue
            ? match(m.value!)
            : @default;
    }

    /// <summary>
    /// Extract the result using the matching function.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <typeparam name="R">The result type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="match">The match function</param>
    /// <param name="default">The default return value if none</param>
    /// <returns>An extracted matched value or default</returns>
    public static async Task<R> MatchValue<T, R>(this Task<Maybe<T>> maybe, Func<T, Task<R>> match, R @default)
    where T : notnull
    where R : notnull
    {
        // both
        var m = await maybe.ConfigureAwait(false);
        return m.hasValue
            ? await match(m.value!).ConfigureAwait(false)
            : @default;
    }

    /// <summary>
    /// Map the maybe to another wrapped maybe value.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <typeparam name="K">The output maybe type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="map">The map function</param>
    /// <returns>A wrapped maybe value</returns>
    public static Maybe<K> Map<T, K>(this Maybe<T> maybe, Func<T, K> map)
    where T : notnull
    where K : notnull
    {
        return maybe.hasValue
            ? map(maybe.value!)
            : Maybe<K>.None;
    }

    /// <summary>
    /// Map the maybe to another wrapped maybe value.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <typeparam name="K">The output maybe type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="map">The map function</param>
    /// <returns>A wrapped maybe value</returns>
    public static async Task<Maybe<K>> Map<T, K>(this Maybe<T> maybe, Func<T, Task<K>> map)
    where T : notnull
    where K : notnull
    {
        // right
        return maybe.hasValue
            ? await map(maybe.value!).ConfigureAwait(false)
            : Maybe<K>.None;
    }

    /// <summary>
    /// Map the maybe to another wrapped maybe value.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <typeparam name="K">The output maybe type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="map">The map function</param>
    /// <returns>A wrapped maybe value</returns>
    public static async Task<Maybe<K>> Map<T, K>(this Task<Maybe<T>> maybe, Func<T, K> map)
    where T : notnull
    where K : notnull
    {
        // left
        var m = await maybe.ConfigureAwait(false);
        return m.hasValue
            ? map(m.value!)
            : Maybe<K>.None;
    }

    /// <summary>
    /// Map the maybe to another wrapped maybe value.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <typeparam name="K">The output maybe type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="map">The map function</param>
    /// <returns>A wrapped maybe value</returns>
    public static async Task<Maybe<K>> Map<T, K>(this Task<Maybe<T>> maybe, Func<T, Task<K>> map)
    where T : notnull
    where K : notnull
    {
        // both
        var m = await maybe.ConfigureAwait(false);
        return m.hasValue
            ? await map(m.value!).ConfigureAwait(false)
            : Maybe<K>.None;
    }

    /// <summary>
    /// If maybe contains a none value, compensate and return some using the compensation function.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="compensate">The compensation function</param>
    /// <returns>A maybe value</returns>
    public static Maybe<T> MapOnError<T>(this Maybe<T> maybe, Func<T> compensate)
    where T : notnull
    {
        return maybe.hasValue
            ? maybe
            : compensate();
    }

    /// <summary>
    /// If maybe contains a none value, compensate and return some using the compensation function.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="compensate">The compensation function</param>
    /// <returns>A maybe value</returns>
    public static async Task<Maybe<T>> MapOnError<T>(this Maybe<T> maybe, Func<Task<T>> compensate)
    where T : notnull
    {
        // right
        return maybe.hasValue
            ? maybe
            : await compensate().ConfigureAwait(false);
    }

    /// <summary>
    /// If maybe contains a none value, compensate and return some using the compensation function.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="compensate">The compensation function</param>
    /// <returns>A maybe value</returns>
    public static async Task<Maybe<T>> MapOnError<T>(this Task<Maybe<T>> maybe, Func<T> compensate)
    where T : notnull
    {
        // left
        var m = await maybe.ConfigureAwait(false);
        return m.hasValue
            ? m
            : compensate();
    }

    /// <summary>
    /// If maybe contains a none value, compensate and return some using the compensation function.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="compensate">The compensation function</param>
    /// <returns>A maybe value</returns>
    public static async Task<Maybe<T>> MapOnError<T>(this Task<Maybe<T>> maybe, Func<Task<T>> compensate)
    where T : notnull
    {
        // both
        var m = await maybe.ConfigureAwait(false);
        return m.hasValue
            ? m
            : await compensate().ConfigureAwait(false);
    }

    /// <summary>
    /// Combine two maybe values into a tuple.
    /// </summary>
    /// <typeparam name="T1">The first wrapped maybe type</typeparam>
    /// <typeparam name="T2">The second wrapped maybe type</typeparam>
    /// <param name="maybe">The first maybe</param>
    /// <param name="other">The second maybe</param>
    /// <returns>The maybe tuple</returns>
    public static Maybe<(T1, T2)> Combine<T1, T2>(this Maybe<T1> maybe, Maybe<T2> other)
    where T1 : notnull
    where T2 : notnull
    {
        return maybe.Bind(t1 => other.Map(t2 => (t1, t2)));
    }

    /// <summary>
    /// Combine two maybe values into a tuple.
    /// </summary>
    /// <typeparam name="T1">The first wrapped maybe type</typeparam>
    /// <typeparam name="T2">The second wrapped maybe type</typeparam>
    /// <param name="maybe">The first maybe</param>
    /// <param name="other">The second maybe</param>
    /// <returns>The maybe tuple</returns>
    public static async Task<Maybe<(T1, T2)>> Combine<T1, T2>(this Task<Maybe<T1>> maybe, Maybe<T2> other)
    where T1 : notnull
    where T2 : notnull
    {
        // left
        return await maybe.Bind(t1 => other.Map(t2 => (t1, t2))).ConfigureAwait(false);
    }

    /// <summary>
    /// Combine two maybe values into a tuple.
    /// </summary>
    /// <typeparam name="T1">The first wrapped maybe type</typeparam>
    /// <typeparam name="T2">The second wrapped maybe type</typeparam>
    /// <param name="maybe">The first maybe</param>
    /// <param name="other">The second maybe</param>
    /// <returns>The maybe tuple</returns>
    public static async Task<Maybe<(T1, T2)>> Combine<T1, T2>(this Maybe<T1> maybe, Task<Maybe<T2>> other)
    where T1 : notnull
    where T2 : notnull
    {
        // right
        return await maybe.Bind(async t1 => await other.Map(t2 => (t1, t2)).ConfigureAwait(false)).ConfigureAwait(false);
    }

    /// <summary>
    /// Combine two maybe values into a tuple.
    /// </summary>
    /// <typeparam name="T1">The first wrapped maybe type</typeparam>
    /// <typeparam name="T2">The second wrapped maybe type</typeparam>
    /// <param name="maybe">The first maybe</param>
    /// <param name="other">The second maybe</param>
    /// <returns>The maybe tuple</returns>
    public static async Task<Maybe<(T1, T2)>> Combine<T1, T2>(this Task<Maybe<T1>> maybe, Task<Maybe<T2>> other)
    where T1 : notnull
    where T2 : notnull
    {
        // both
        return await maybe.Bind(async t1 => await other.Map(t2 => (t1, t2)).ConfigureAwait(false)).ConfigureAwait(false);
    }

    /// <summary>
    /// Combine two maybe values by function.
    /// </summary>
    /// <typeparam name="T1">The first wrapped maybe type</typeparam>
    /// <typeparam name="T2">The second wrapped maybe type</typeparam>
    /// <typeparam name="R">The return type</typeparam>
    /// <param name="maybe">The first maybe</param>
    /// <param name="other">The second maybe</param>
    /// <param name="combine">The combination function</param>
    /// <returns>The maybe tuple</returns>
    public static Maybe<R> Combine<T1, T2, R>(this Maybe<T1> maybe, Maybe<T2> other, Func<T1, T2, R> combine)
    where T1 : notnull
    where T2 : notnull
    where R : notnull
    {
        return maybe.Bind(t1 => other.Map(t2 => combine(t1, t2)));
    }

    /// <summary>
    /// Combine two maybe values by function.
    /// </summary>
    /// <typeparam name="T1">The first wrapped maybe type</typeparam>
    /// <typeparam name="T2">The second wrapped maybe type</typeparam>
    /// <typeparam name="R">The return type</typeparam>
    /// <param name="maybe">The first maybe</param>
    /// <param name="other">The second maybe</param>
    /// <param name="combine">The combination function</param>
    /// <returns>The maybe tuple</returns>
    public static Task<Maybe<R>> Combine<T1, T2, R>(this Maybe<T1> maybe, Maybe<T2> other, Func<T1, T2, Task<R>> combine)
    where T1 : notnull
    where T2 : notnull
    where R : notnull
    {
        // right
        return maybe.Bind(t1 => other.Map(async t2 => await combine(t1, t2).ConfigureAwait(false)));
    }

    /// <summary>
    /// Combine two maybe values by function.
    /// </summary>
    /// <typeparam name="T1">The first wrapped maybe type</typeparam>
    /// <typeparam name="T2">The second wrapped maybe type</typeparam>
    /// <typeparam name="R">The return type</typeparam>
    /// <param name="maybe">The first maybe</param>
    /// <param name="other">The second maybe</param>
    /// <param name="combine">The combination function</param>
    /// <returns>The maybe tuple</returns>
    public static async Task<Maybe<R>> Combine<T1, T2, R>(this Task<Maybe<T1>> maybe, Maybe<T2> other, Func<T1, T2, R> combine)
    where T1 : notnull
    where T2 : notnull
    where R : notnull
    {
        // left
        return await maybe.Bind(t1 => other.Map(t2 => combine(t1, t2))).ConfigureAwait(false);
    }

    /// <summary>
    /// Combine two maybe values by function.
    /// </summary>
    /// <typeparam name="T1">The first wrapped maybe type</typeparam>
    /// <typeparam name="T2">The second wrapped maybe type</typeparam>
    /// <typeparam name="R">The return type</typeparam>
    /// <param name="maybe">The first maybe</param>
    /// <param name="other">The second maybe</param>
    /// <param name="combine">The combination function</param>
    /// <returns>The maybe tuple</returns>
    public static Task<Maybe<R>> Combine<T1, T2, R>(this Task<Maybe<T1>> maybe, Maybe<T2> other, Func<T1, T2, Task<R>> combine)
    where T1 : notnull
    where T2 : notnull
    where R : notnull
    {
        // both
        return maybe.Bind(t1 => other.Map(async t2 => await combine(t1, t2).ConfigureAwait(false)));
    }

    /// <summary>
    /// Execute action when maybe has some value.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="action">The action to execute</param>
    /// <returns>The original maybe value</returns>
    public static Maybe<T> Peek<T>(this Maybe<T> maybe, Action<T> action)
    where T : notnull
    {
        if (maybe)
        {
            action(maybe.value!);
        }

        return maybe;
    }

    /// <summary>
    /// Execute action when maybe has some value.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="action">The action to execute</param>
    /// <returns>The original maybe value</returns>
    public static async Task<Maybe<T>> Peek<T>(this Maybe<T> maybe, Func<T, Task> action)
    where T : notnull
    {
        // right
        if (maybe)
        {
            await action(maybe.value!).ConfigureAwait(false);
        }

        return maybe;
    }

    /// <summary>
    /// Execute action when maybe has some value.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="action">The action to execute</param>
    /// <returns>The original maybe value</returns>
    public static async Task<Maybe<T>> Peek<T>(this Task<Maybe<T>> maybe, Action<T> action)
    where T : notnull
    {
        // left
        var m = await maybe.ConfigureAwait(false);
        if (m)
        {
            action(m.value!);
        }

        return m;
    }

    /// <summary>
    /// Execute action when maybe has some value.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="action">The action to execute</param>
    /// <returns>The original maybe value</returns>
    public static async Task<Maybe<T>> Peek<T>(this Task<Maybe<T>> maybe, Func<T, Task> action)
    where T : notnull
    {
        // both
        var m = await maybe.ConfigureAwait(false);
        if (m)
        {
            await action(m.value!).ConfigureAwait(false);
        }

        return m;
    }

    /// <summary>
    /// Deconstruct into a boolean indicating if maybe has some value and an optional value.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="hasValue">True if maybe has some value. Otherwise false.</param>
    /// <param name="some">The optional wrapped value or null.</param>
    public static void Deconstruct<T>(this Maybe<T> maybe, out bool hasValue, out T? some)
    where T : struct
    {
        hasValue = maybe.HasValue;
        some = hasValue ? maybe.value : null;
    }

    /// <summary>
    /// Deconstruct into a boolean indicating if maybe has some value and an optional value.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="hasValue">True if maybe has some value. Otherwise false.</param>
    /// <param name="some">The optional wrapped value or null.</param>
    public static void Deconstruct<T>(this Maybe<T> maybe, out bool hasValue, out T? some)
    where T : class
    {
        hasValue = maybe.HasValue;
        some = hasValue ? maybe.value : null;
    }
}

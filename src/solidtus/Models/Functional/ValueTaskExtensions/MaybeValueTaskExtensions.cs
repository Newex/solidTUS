using System;
using System.Threading.Tasks;

namespace SolidTUS.Models.Functional.ValueTaskExtensions;

/// <summary>
/// Extensions for functional <see cref="Maybe{T}"/> <see cref="ValueTask"/>
/// </summary>
internal static class MaybeValueTaskExtensions
{
    /// <summary>
    /// Flat map a maybe.
    /// </summary>
    /// <typeparam name="T">The input type</typeparam>
    /// <typeparam name="K">The output type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="bindValueTask">The flat map function</param>
    /// <returns>A maybe value</returns>
    public static async ValueTask<Maybe<K>> Bind<T, K>(this Maybe<T> maybe, Func<T, ValueTask<Maybe<K>>> bindValueTask)
    where T : notnull
    where K : notnull
    {
        // right
        return maybe.HasValue
            ? await bindValueTask(maybe.value!)
            : Maybe<K>.None;
    }

    /// <summary>
    /// Flat map a maybe.
    /// </summary>
    /// <typeparam name="T">The input type</typeparam>
    /// <typeparam name="K">The output type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="bindValueTask">The flat map function</param>
    /// <returns>A maybe value</returns>
    public static async ValueTask<Maybe<K>> Bind<T, K>(this ValueTask<Maybe<T>> maybe, Func<T, Maybe<K>> bindValueTask)
    where T : notnull
    where K : notnull
    {
        // left
        var m = await maybe;
        return m.HasValue
            ? bindValueTask(m.value!)
            : Maybe<K>.None;
    }

    /// <summary>
    /// Flat map a maybe.
    /// </summary>
    /// <typeparam name="T">The input type</typeparam>
    /// <typeparam name="K">The output type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="bindValueTask">The flat map function</param>
    /// <returns>A maybe value</returns>
    public static async ValueTask<Maybe<K>> Bind<T, K>(this ValueTask<Maybe<T>> maybe, Func<T, ValueTask<Maybe<K>>> bindValueTask)
    where T : notnull
    where K : notnull
    {
        // both
        var m = await maybe;
        return m.HasValue
            ? await bindValueTask(m.value!)
            : Maybe<K>.None;
    }

    /// <summary>
    /// Map a maybe.
    /// </summary>
    /// <typeparam name="T">The input type</typeparam>
    /// <typeparam name="K">The output type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="map">The map function</param>
    /// <returns>A maybe value</returns>
    public static async ValueTask<Maybe<K>> Map<T, K>(this Maybe<T> maybe, Func<T, ValueTask<K>> map)
    where T : notnull
    where K : notnull
    {
        // right
        return maybe.HasValue
            ? await map(maybe.value!)
            : Maybe<K>.None;
    }

    /// <summary>
    /// Map a maybe.
    /// </summary>
    /// <typeparam name="T">The input type</typeparam>
    /// <typeparam name="K">The output type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="map">The map function</param>
    /// <returns>A maybe value</returns>
    public static async ValueTask<Maybe<K>> Map<T, K>(this ValueTask<Maybe<T>> maybe, Func<T, K> map)
    where T : notnull
    where K : notnull
    {
        // left
        var m = await maybe;
        return m.HasValue
            ? map(m.value!)
            : Maybe<K>.None;
    }

    /// <summary>
    /// Map a maybe.
    /// </summary>
    /// <typeparam name="T">The input type</typeparam>
    /// <typeparam name="K">The output type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="map">The map function</param>
    /// <returns>A maybe value</returns>
    public static async ValueTask<Maybe<K>> Map<T, K>(this ValueTask<Maybe<T>> maybe, Func<T, ValueTask<K>> map)
    where T : notnull
    where K : notnull
    {
        // both
        var m = await maybe;
        return m.HasValue
            ? await map(m.value!)
            : Maybe<K>.None;
    }

    /// <summary>
    /// Combine two maybes to a tuple.
    /// </summary>
    /// <typeparam name="T1">The first maybe value type</typeparam>
    /// <typeparam name="T2">The second maybe value type</typeparam>
    /// <param name="maybe">The first maybe value</param>
    /// <param name="other">The second maybe value</param>
    /// <returns>A maybe tuple</returns>
    public static async ValueTask<Maybe<(T1, T2)>> Combine<T1, T2>(this ValueTask<Maybe<T1>> maybe, Maybe<T2> other)
    where T1 : notnull
    where T2 : notnull
    {
        // left
        return await maybe.Bind(t1 => other.Map(t2 => (t1, t2)));
    }

    /// <summary>
    /// Combine two maybes to a tuple.
    /// </summary>
    /// <typeparam name="T1">The first maybe value type</typeparam>
    /// <typeparam name="T2">The second maybe value type</typeparam>
    /// <param name="maybe">The first maybe value</param>
    /// <param name="other">The second maybe value</param>
    /// <returns>A maybe tuple</returns>
    public static async ValueTask<Maybe<(T1, T2)>> Combine<T1, T2>(this Maybe<T1> maybe, ValueTask<Maybe<T2>> other)
    where T1 : notnull
    where T2 : notnull
    {
        // right
        return await maybe.Bind(async t1 => await other.Map(t2 => (t1, t2)));
    }

    /// <summary>
    /// Combine two maybes to a tuple.
    /// </summary>
    /// <typeparam name="T1">The first maybe value type</typeparam>
    /// <typeparam name="T2">The second maybe value type</typeparam>
    /// <param name="maybe">The first maybe value</param>
    /// <param name="other">The second maybe value</param>
    /// <returns>A maybe tuple</returns>
    public static async ValueTask<Maybe<(T1, T2)>> Combine<T1, T2>(this ValueTask<Maybe<T1>> maybe, ValueTask<Maybe<T2>> other)
    where T1 : notnull
    where T2 : notnull
    {
        // both
        return await maybe.Bind(async t1 => await other.Map(t2 => (t1, t2)));
    }

    /// <summary>
    /// Combine two maybes using a combination function.
    /// </summary>
    /// <typeparam name="T1">The first maybe value type</typeparam>
    /// <typeparam name="T2">The second maybe value type</typeparam>
    /// <typeparam name="R">The wrapped return type</typeparam>
    /// <param name="maybe">The first maybe value</param>
    /// <param name="other">The second maybe value</param>
    /// <param name="combine">The combination function</param>
    /// <returns>A maybe tuple</returns>
    public static async ValueTask<Maybe<R>> Combine<T1, T2, R>(this ValueTask<Maybe<T1>> maybe, Maybe<T2> other, Func<T1, T2, R> combine)
    where T1 : notnull
    where T2 : notnull
    where R : notnull
    {
        // left
        return await maybe.Bind(t1 => other.Map(t2 => combine(t1, t2)));
    }

    /// <summary>
    /// Combine two maybes using a combination function.
    /// </summary>
    /// <typeparam name="T1">The first maybe value type</typeparam>
    /// <typeparam name="T2">The second maybe value type</typeparam>
    /// <typeparam name="R">The wrapped return type</typeparam>
    /// <param name="maybe">The first maybe value</param>
    /// <param name="other">The second maybe value</param>
    /// <param name="combine">The combination function</param>
    /// <returns>A maybe tuple</returns>
    public static async ValueTask<Maybe<R>> Combine<T1, T2, R>(this Maybe<T1> maybe, ValueTask<Maybe<T2>> other, Func<T1, T2, R> combine)
    where T1 : notnull
    where T2 : notnull
    where R : notnull
    {
        // right
        return await maybe.Bind(async t1 => await other.Map(t2 => combine(t1, t2)));
    }

    /// <summary>
    /// Combine two maybes using a combination function.
    /// </summary>
    /// <typeparam name="T1">The first maybe value type</typeparam>
    /// <typeparam name="T2">The second maybe value type</typeparam>
    /// <typeparam name="R">The wrapped return type</typeparam>
    /// <param name="maybe">The first maybe value</param>
    /// <param name="other">The second maybe value</param>
    /// <param name="combine">The combination function</param>
    /// <returns>A maybe tuple</returns>
    public static async ValueTask<Maybe<R>> Combine<T1, T2, R>(this ValueTask<Maybe<T1>> maybe, ValueTask<Maybe<T2>> other, Func<T1, T2, R> combine)
    where T1 : notnull
    where T2 : notnull
    where R : notnull
    {
        // both
        return await maybe.Bind(async t1 => await other.Map(t2 => combine(t1, t2)));
    }

    /// <summary>
    /// Combine two maybes using a combination function.
    /// </summary>
    /// <typeparam name="T1">The first maybe value type</typeparam>
    /// <typeparam name="T2">The second maybe value type</typeparam>
    /// <typeparam name="R">The wrapped return type</typeparam>
    /// <param name="maybe">The first maybe value</param>
    /// <param name="other">The second maybe value</param>
    /// <param name="combine">The combination function</param>
    /// <returns>A maybe tuple</returns>
    public static async ValueTask<Maybe<R>> Combine<T1, T2, R>(this Maybe<T1> maybe, Maybe<T2> other, Func<T1, T2, ValueTask<R>> combine)
    where T1 : notnull
    where T2 : notnull
    where R : notnull
    {
        // right
        return await maybe.Bind(t1 => other.Map(async t2 => await combine(t1, t2)));
    }


    /// <summary>
    /// Combine two maybes using a combination function.
    /// </summary>
    /// <typeparam name="T1">The first maybe value type</typeparam>
    /// <typeparam name="T2">The second maybe value type</typeparam>
    /// <typeparam name="R">The wrapped return type</typeparam>
    /// <param name="maybe">The first maybe value</param>
    /// <param name="other">The second maybe value</param>
    /// <param name="combine">The combination function</param>
    /// <returns>A maybe tuple</returns>
    public static async ValueTask<Maybe<R>> Combine<T1, T2, R>(this ValueTask<Maybe<T1>> maybe, Maybe<T2> other, Func<T1, T2, ValueTask<R>> combine)
    where T1 : notnull
    where T2 : notnull
    where R : notnull
    {
        // left - right
        return await maybe.Bind(t1 => other.Map(async t2 => await combine(t1, t2)));
    }

    /// <summary>
    /// Combine two maybes using a combination function.
    /// </summary>
    /// <typeparam name="T1">The first maybe value type</typeparam>
    /// <typeparam name="T2">The second maybe value type</typeparam>
    /// <typeparam name="R">The wrapped return type</typeparam>
    /// <param name="maybe">The first maybe value</param>
    /// <param name="other">The second maybe value</param>
    /// <param name="combine">The combination function</param>
    /// <returns>A maybe tuple</returns>
    public static async ValueTask<Maybe<R>> Combine<T1, T2, R>(this Maybe<T1> maybe, ValueTask<Maybe<T2>> other, Func<T1, T2, ValueTask<R>> combine)
    where T1 : notnull
    where T2 : notnull
    where R : notnull
    {
        // center - right
        return await maybe.Bind(t1 => other.Map(t2 => combine(t1, t2)));
    }

    /// <summary>
    /// Combine two maybes using a combination function.
    /// </summary>
    /// <typeparam name="T1">The first maybe value type</typeparam>
    /// <typeparam name="T2">The second maybe value type</typeparam>
    /// <typeparam name="R">The wrapped return type</typeparam>
    /// <param name="maybe">The first maybe value</param>
    /// <param name="other">The second maybe value</param>
    /// <param name="combine">The combination function</param>
    /// <returns>A maybe tuple</returns>
    public static async ValueTask<Maybe<R>> Combine<T1, T2, R>(this ValueTask<Maybe<T1>> maybe, ValueTask<Maybe<T2>> other, Func<T1, T2, ValueTask<R>> combine)
    where T1 : notnull
    where T2 : notnull
    where R : notnull
    {
        // left - center - right
        return await maybe.Bind(t1 => other.Map(async t2 => await combine(t1, t2)));
    }

    /// <summary>
    /// Compensate a none maybe to some maybe.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe value type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="compensate">The compensation function, called if maybe is none.</param>
    /// <returns>A new maybe if this maybe contains no value or the same maybe value if it contains some</returns>
    public static async ValueTask<Maybe<T>> Compensate<T>(this Maybe<T> maybe, Func<ValueTask<T>> compensate)
    where T : notnull
    {
        // right
        return !maybe.HasValue
            ? await compensate()
            : maybe;
    }

    /// <summary>
    /// Compensate a none maybe to some maybe.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe value type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="compensate">The compensation function, called if maybe is none.</param>
    /// <returns>A new maybe if this maybe contains no value or the same maybe value if it contains some</returns>
    public static async ValueTask<Maybe<T>> Compensate<T>(this ValueTask<Maybe<T>> maybe, Func<T> compensate)
    where T : notnull
    {
        // left
        var m = await maybe;
        return !m.HasValue
            ? compensate()
            : m;
    }

    /// <summary>
    /// Compensate a none maybe to some maybe.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe value type</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="compensate">The compensation function, called if maybe is none.</param>
    /// <returns>A new maybe if this maybe contains no value or the same maybe value if it contains some</returns>
    public static async ValueTask<Maybe<T>> Compensate<T>(this ValueTask<Maybe<T>> maybe, Func<ValueTask<T>> compensate)
    where T : notnull
    {
        // both
        var m = await maybe;
        return !m.HasValue
            ? await compensate()
            : m;
    }

    /// <summary>
    /// Peek at the current value and execute an action without changing the original value.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe value</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="peek">The action function</param>
    /// <returns>The original maybe value</returns>
    public static async ValueTask<Maybe<T>> Peek<T>(this Maybe<T> maybe, Func<T, ValueTask> peek)
    where T : notnull
    {
        // right
        if (maybe.HasValue)
        {
            await peek(maybe.value!);
        }

        return maybe;
    }

    /// <summary>
    /// Peek at the current value and execute an action without changing the original value.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe value</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="peek">The action function</param>
    /// <returns>The original maybe value</returns>
    public static async ValueTask<Maybe<T>> Peek<T>(this ValueTask<Maybe<T>> maybe, Action<T> peek)
    where T : notnull
    {
        // left
        var m = await maybe;
        if (m.HasValue)
        {
            peek(m.value!);
        }

        return m;
    }

    /// <summary>
    /// Peek at the current value and execute an action without changing the original value.
    /// </summary>
    /// <typeparam name="T">The wrapped maybe value</typeparam>
    /// <param name="maybe">The maybe value</param>
    /// <param name="peek">The action function</param>
    /// <returns>The original maybe value</returns>
    public static async ValueTask<Maybe<T>> Peek<T>(this ValueTask<Maybe<T>> maybe, Func<T, ValueTask> peek)
    where T : notnull
    {
        // both
        var m = await maybe;
        if (m.HasValue)
        {
            await peek(m.value!);
        }

        return m;
    }
}

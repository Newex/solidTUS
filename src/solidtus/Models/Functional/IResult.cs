using System;

namespace SolidTUS.Models.Functional;

/// <summary>
/// A covariant result interface.
/// </summary>
/// <typeparam name="T">The success type</typeparam>
/// <typeparam name="E">The error type</typeparam>
internal interface IResult<out T, out E>
where T : notnull
{
    /// <summary>
    /// Extract the result using either the success or error functions.
    /// </summary>
    /// <typeparam name="R">The return type</typeparam>
    /// <param name="onSuccess">The success function</param>
    /// <param name="onError">The error function</param>
    /// <returns>An extracted result value</returns>
    R? Extract<R>(Func<T, R> onSuccess, Func<E, R> onError);
}

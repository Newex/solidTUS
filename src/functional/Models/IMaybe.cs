using System;

namespace SolidTUS.Functional.Models;

/// <summary>
/// Maybe covariant interface
/// </summary>
/// <typeparam name="T">The wrapped maybe type</typeparam>
public interface IMaybe<out T>
{
    /// <summary>
    /// Extract the maybe values by some or none functions.
    /// </summary>
    /// <typeparam name="R">The output result type</typeparam>
    /// <param name="some">The some function</param>
    /// <param name="none">The none function</param>
    /// <returns>A value</returns>
    R? Extract<R>(Func<T, R> some, Func<R> none);
}

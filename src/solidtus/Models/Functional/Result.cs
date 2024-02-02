using System;
using System.Diagnostics;

namespace SolidTUS.Models.Functional;

/// <summary>
/// A result representing a success or an error.
/// </summary>
/// <typeparam name="T">The success type</typeparam>
/// <typeparam name="E">The error type</typeparam>
[DebuggerDisplay("{DebugDisplay,nq}")]
public readonly record struct Result<T, E> : IResult<T, E>
    where T : notnull
{
    internal readonly T? value;
    internal readonly E? error;

    private readonly bool isSuccess;

    /// <summary>
    /// Do not instantiate a result directly, use <see cref="Success(T)"/> or <see cref="Error(E)"/> to instantiate a new result.
    /// </summary>
    /// <exception cref="InvalidOperationException">Always thrown if called</exception>
    [Obsolete("Use Success or Error static method to instantiate a result. Throws InvalidOperationException.")]
    public Result()
    {
        throw new InvalidOperationException("Use static method, Success or Error to instantiate a new result.");
    }

    private Result(T? success, E? error, bool isSuccess)
    {
        value = success;
        this.error = error;
        this.isSuccess = isSuccess;
    }

    /// <summary>
    /// Get if the result is a success.
    /// </summary>
    public readonly bool IsSuccess => isSuccess;

    /// <inheritdoc cref="Equals(object)"/>
    public bool Equals(T other) => value?.Equals(other) ?? false;

    /// <summary>
    /// Equality comparison.
    /// </summary>
    /// <param name="left">The left operand</param>
    /// <param name="right">The right operand</param>
    /// <returns>True if the result is a success with a value that equals <paramref name="left"/> operand. Otherwise false.</returns>
    public static bool operator ==(T left, Result<T, E> right)
    {
        return right.isSuccess && right.value!.Equals(left);
    }

    /// <summary>
    /// Equality comparison.
    /// </summary>
    /// <param name="left">The left operand</param>
    /// <param name="right">The right operand</param>
    /// <returns>True if the result is a success with a value that equals <paramref name="right"/> operand. Otherwise false.</returns>
    public static bool operator ==(Result<T, E> left, T right)
    {
        return left.isSuccess && left.value!.Equals(right);
    }

    /// <summary>
    /// Equality comparison.
    /// </summary>
    /// <param name="left">The left operand</param>
    /// <param name="right">The right operand</param>
    /// <returns>True if the result is a success with a value that does not equals <paramref name="right"/> operand. Otherwise false.</returns>
    public static bool operator !=(Result<T, E> left, T right)
    {
        return !left.isSuccess || !left.value!.Equals(right);
    }

    /// <summary>
    /// Equality comparison.
    /// </summary>
    /// <param name="left">The left operand</param>
    /// <param name="right">The right operand</param>
    /// <returns>False if the right operand is a succes with a value that equals <paramref name="left"/> operand. Otherwise true.</returns>
    public static bool operator !=(T left, Result<T, E> right)
    {
        return !right.isSuccess || !right.value!.Equals(left);
    }

    /// <summary>
    /// Implicit convert value to a result. If the input is null, the result will be an error, with a default error value.
    /// </summary>
    /// <param name="input">The success input value</param>
    public static implicit operator Result<T, E>(T? input) => input is not null ? Success(input) : Error(default!);

    /// <summary>
    /// Implicitly convert an error value to a result.
    /// </summary>
    /// <param name="input">The error input value</param>
    public static implicit operator Result<T, E>(E input) => Error(input);

    /// <summary>
    /// Implicitly convert result to a boolean.
    /// </summary>
    /// <param name="input">The result input value</param>
    public static implicit operator bool(Result<T, E> input) => input.isSuccess;

    /// <summary>
    /// Implicit convert result to a maybe value.
    /// </summary>
    /// <param name="input">The result input value</param>
    public static implicit operator Maybe<T>(Result<T, E> input) => input.isSuccess ? Maybe<T>.Some(input.value!) : Maybe.None;

    /// <summary>
    /// Initialize a success result.
    /// </summary>
    /// <param name="value">The success value</param>
    /// <returns>A success result</returns>
    public static Result<T, E> Success(T value)
    {
        return new Result<T, E>(value, default, true);
    }

    /// <summary>
    /// Initialize an error result.
    /// </summary>
    /// <param name="error">The error value</param>
    /// <returns>An error result</returns>
    public static Result<T, E> Error(E error)
    {
        return new Result<T, E>(default, error, false);
    }

    /// <inheritdoc />
    public R? Extract<R>(Func<T, R> onSuccess, Func<E, R> onError)
    {
        return isSuccess
            ? onSuccess(value!)
            : onError(error!);
    }


    private string DebugDisplay
    {
        get
        {
            return isSuccess
                ? string.Format("Success: {0}", value)
                : string.Format("Error: {0}", error);
        }
    }
}

using System;
using System.Diagnostics;

namespace SolidTUS.Models.Functional;

/// <summary>
/// A maybe result containing either some value or none. If the wrapped value is a null value the maybe will be none.
/// </summary>
/// <typeparam name="T">The wrapped maybe type</typeparam>
[DebuggerDisplay("{DebugDisplay,nq}")]
internal readonly record struct Maybe<T> : IMaybe<T>
where T : notnull
{
    internal readonly T? value;
    internal readonly bool hasValue;

    /// <summary>
    /// Default constructor creates a None value.
    /// </summary>
    public Maybe()
    {
        value = default;
        hasValue = false;
    }

    private Maybe(T? input)
    {
        value = input;
        hasValue = input is not null;
    }

    /// <summary>
    /// Create some value.
    /// </summary>
    /// <param name="input">The wrapped input maybe value</param>
    /// <returns>A maybe value</returns>
    public static Maybe<T> Some(T input)
    {
        return new Maybe<T>(input);
    }

    /// <summary>
    /// Create none value.
    /// </summary>
    /// <returns>A maybe value</returns>
    public static Maybe<T> None
    {
        get
        {
            return new();
        }
    }

    /// <summary>
    /// True if this maybe contains a wrapped value. Otherwise false.
    /// </summary>
    public readonly bool HasValue => hasValue;

    /// <inheritdoc cref="Equals(object)"/>
    public bool Equals(T other) => value?.Equals(other) ?? false;

    /// <inheritdoc />
    public R? Extract<R>(Func<T, R> some, Func<R> none)
    {
        return hasValue
            ? some(value!)
            : none();
    }


    /// <summary>
    /// Equality comparison.
    /// </summary>
    /// <param name="left">The left operand</param>
    /// <param name="right">The right operand</param>
    /// <returns>True if the maybe contains a value that is equal to the left operand. Otherwise false.</returns>
    public static bool operator ==(T left, Maybe<T> right)
    {
        return right.hasValue && left.Equals(right.value);
    }

    /// <summary>
    /// Equality comparison.
    /// </summary>
    /// <param name="left">The left operand</param>
    /// <param name="right">The right operand</param>
    /// <returns>True if the maybe contains a value that is not equal to the left operand. Otherwise false.</returns>
    public static bool operator !=(T left, Maybe<T> right)
    {
        return !right.hasValue || !left.Equals(right.value);
    }

    /// <summary>
    /// Equality comparison.
    /// </summary>
    /// <param name="left">The left operand</param>
    /// <param name="right">The right operand</param>
    /// <returns>True if the maybe contains a value that is equal to the right operand. Otherwise false.</returns>
    public static bool operator ==(Maybe<T> left, T right)
    {
        return left.hasValue && right.Equals(left.value);
    }

    /// <summary>
    /// Equality comparison.
    /// </summary>
    /// <param name="left">The left operand</param>
    /// <param name="right">The right operand</param>
    /// <returns>True if the maybe contains a value that is not equal to the right operand. Otherwise false.</returns>
    public static bool operator !=(Maybe<T> left, T right)
    {
        return !left.hasValue || !right.Equals(left.value);
    }

    /// <summary>
    /// Implicit conversion from a value to a maybe type.
    /// </summary>
    /// <param name="input">The input value</param>
    public static implicit operator Maybe<T>(T? input) => new(input);

    /// <summary>
    /// Implicit conversion from a maybe to a boolean value.
    /// </summary>
    /// <param name="input">The maybe value</param>
    public static implicit operator bool(Maybe<T> input) => input.hasValue;

    /// <summary>
    /// Implicit conversion from maybe to a none value.
    /// </summary>
    /// <param name="_">The maybe value</param>
    public static implicit operator Maybe<T>(Maybe _) => new();

    private string DebugDisplay
    {
        get
        {
            return hasValue
                ? string.Format("Some: {0}", value)
                : string.Format("None");
        }
    }
}

/// <inheritdoc cref="Maybe{T}"/>
public record struct Maybe
{
    /// <summary>
    /// None value
    /// </summary>
    public static Maybe None
    {
        get
        {
            return new();
        }
    }
}
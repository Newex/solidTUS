using System;
using System.Diagnostics.CodeAnalysis;

namespace SolidTUS.Extensions;

/// <summary>
/// Extensions for generic types
/// </summary>
public static class GenericTypeExtensions
{
    /// <summary>
    /// Alternative version of <see cref="Type.IsSubclassOf"/> that supports raw generic types (generic types without
    /// any type parameters).
    /// </summary>
    /// <remarks>source: https://extensionmethod.net/csharp/type/issubclassofrawgeneric</remarks>
    /// <param name="baseType">The base type class for which the check is made.</param>
    /// <param name="toCheck">To type to determine for whether it derives from <paramref name="baseType"/>.</param>
    public static bool IsSubclassOfRawGeneric([NotNull] this Type toCheck, Type baseType)
    {
        while (toCheck != typeof(object))
        {
            Type cur = toCheck!.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
            if (baseType == cur)
            {
                return true;
            }

            toCheck = toCheck.BaseType!;
        }

        return false;
    }
}

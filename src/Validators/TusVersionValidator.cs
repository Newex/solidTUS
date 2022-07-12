using System;
using System.Linq;
using Microsoft.Extensions.Primitives;
using SolidTUS.Constants;

namespace SolidTUS.Validators;

/// <summary>
/// Tus verrsion validator
/// </summary>
internal static class TusVersionValidator
{
    /// <summary>
    /// Validates if the given version is supported by the server
    /// </summary>
    /// <param name="version">The given version</param>
    /// <returns>True if supported otherwise false</returns>
    public static bool IsValidVersion(StringValues version)
    {
        return TusHeaderValues
            .TusServerVersions
            .Split(",")
            .Any(v => v.Equals(version, StringComparison.OrdinalIgnoreCase));
    }
}

using System;
using System.Text;

namespace SolidTUS.Parsers;

/// <summary>
/// Base64 converter
/// </summary>
public static class Base64Converters
{
    /// <summary>
    /// Encode an input string to base64 encoding
    /// </summary>
    /// <param name="input">The input string</param>
    /// <returns>A base64 encoded string</returns>
    public static string Encode(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Decode a base64 encoded string
    /// </summary>
    /// <param name="encoded">The encoded string</param>
    /// <returns>A decoded string</returns>
    public static string Decode(string encoded)
    {
        var bytes = Convert.FromBase64String(encoded);
        return Encoding.UTF8.GetString(bytes);
    }
}

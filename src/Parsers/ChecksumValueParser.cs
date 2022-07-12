using System;

namespace SolidTUS.Parsers;

/// <summary>
/// Checksum value parser
/// </summary>
public static class ChecksumValueParser
{
    /// <summary>
    /// Decode checksum TUS input
    /// </summary>
    /// <param name="input">The checksum value input</param>
    /// <returns>A tuple with the algorithm name and the corresponding cipher value or null</returns>
    /// <exception cref="ArgumentException">Thrown when invalid input</exception>
    public static (string AlgorithmName, byte[] Cipher)? DecodeCipher(string input)
    {
        var split = input.Split(" ");
        if (split.Length != 2)
        {
            // throw new ArgumentException("Invalid cipher input", nameof(input));
            return null;
        }

        var name = split[0];
        var cipher = split[1];

        try
        {
            var bytes = Convert.FromBase64String(cipher);
            return (name, bytes);
        }
        catch (FormatException)
        {
            return null;
        }
        catch (Exception)
        {
            throw;
        }
    }
}

using System;
using System.Collections.Generic;

namespace SolidTUS.Parsers;

/// <summary>
/// TUS Metadata parser
/// </summary>
public static class MetadataParser
{
    /// <summary>
    /// Parses TUS metadata
    /// </summary>
    /// <param name="metadata">The raw TUS metadata</param>
    /// <returns>A collection of key value pairs</returns>
    /// <exception cref="InvalidOperationException">Invalid metadata and cannot parse it</exception>
    public static Dictionary<string, string> Parse(string metadata)
    {
        var keyValues = metadata.Split(",");

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var keyValue in keyValues)
        {
            var item = keyValue.Split(" ");

            if (item.Length == 2)
            {
                var key = item[0];
                var value = item[1];
                var decode = Base64Converters.Decode(value);
                result.Add(key, decode);
            }
            else if (item.Length == 1)
            {
                var key = item[0];
                var value = string.Empty;
                result.Add(key, value);
            }
            else
            {
                throw new InvalidOperationException("Invalid TUS-Metadata header values");
            }
        }

        return result;
    }
}

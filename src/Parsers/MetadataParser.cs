using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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

    /// <summary>
    /// Fast parsing using unsafe and stack allocated strings
    /// </summary>
    /// <param name="metadata">The metadata</param>
    /// <returns>A dictionary of decoded metadata</returns>
    public static Dictionary<string, string> ParseFast(string metadata)
    {
        try
        {
            var result = new Dictionary<string, string>();

            ReadOnlySpan<char> chars = metadata;

            int start = 0;
            int stride = 0;

            var hasValue = false;
            var processed = false;
            ReadOnlySpan<char> key = stackalloc char[metadata.Length];
            ReadOnlySpan<char> value = stackalloc char[metadata.Length];

            for (int i = 0; i < chars.Length; i++)
            {
                var token = chars[i];
                if (token != ' ' && token != ',' && i != (chars.Length - 1))
                {
                    stride++;
                    continue;
                }
                else if (token == ' ')
                {
                    key = chars.Slice(start, stride);
                    start = i + 1;
                    stride = 0;
                    hasValue = true;
                    continue;
                }

                if (token == ',' || i == (chars.Length - 1))
                {
                    if (i == (chars.Length - 1))
                    {
                        // To the end
                        stride++;
                    }

                    if (hasValue)
                    {
                        value = chars.Slice(start, stride);
                    }
                    else
                    {
                        key = chars.Slice(start, stride);
                    }

                    processed = true;
                    start = i + 1;
                    stride = 0;
                }

                if (processed && hasValue)
                {
                    ref var val = ref CollectionsMarshal.GetValueRefOrAddDefault(result, key.ToString(), out var exist);
                    if (!exist)
                    {
                        var decode = Base64Converters.Decode(value.ToString());
                        val = decode;
                    }

                    processed = false;
                    hasValue = false;
                } else if (processed)
                {
                    ref var val = ref CollectionsMarshal.GetValueRefOrAddDefault(result, key.ToString(), out var exist);
                    val = string.Empty;

                    processed = false;
                    hasValue = false;
                }
            }

            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }
}

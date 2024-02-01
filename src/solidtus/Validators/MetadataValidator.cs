using System.Collections.Generic;

namespace SolidTUS.Validators;

internal static class MetadataValidator
{
    public static bool AllowEmptyMetadata() => true;

    public static bool Validator(Dictionary<string, string> metadata) => true;
}

/// <summary>
/// Validates the parsed <c>Upload-Metadata</c> header.
/// </summary>
/// <param name="metadata">The parsed metadata values</param>
/// <returns>True if valid otherwise false</returns>
public delegate bool MetadataValidatorFunc(Dictionary<string, string> metadata);

/// <summary>
/// A function to determine if <c>Upload-Metadata</c> should be allowed to be omitted.
/// </summary>
/// <returns>True if metadata can be omitted otherwise false</returns>
public delegate bool AllowEmptyMetadataFunc();
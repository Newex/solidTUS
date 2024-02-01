using System;
using System.Collections.Generic;
using CSharpFunctionalExtensions;
using SolidTUS.Models;
using SolidTUS.Validators;

namespace SolidTUS.Parsers;

/// <summary>
/// TUS Metadata parser
/// </summary>
public class MetadataParser
{
    private readonly MetadataValidatorFunc metadataValidator;
    private readonly AllowEmptyMetadataFunc allowEmpty;

    /// <summary>
    /// Instantiate a new metadata parser
    /// </summary>
    /// <param name="metadataValidator">The metadata validator</param>
    /// <param name="allowEmpty"></param>
    public MetadataParser(MetadataValidatorFunc metadataValidator, AllowEmptyMetadataFunc allowEmpty)
    {
        this.metadataValidator = metadataValidator;
        this.allowEmpty = allowEmpty;
    }

    /// <summary>
    /// Parses TUS metadata
    /// </summary>
    /// <param name="metadata">The raw TUS metadata</param>
    /// <returns>A collection of key value pairs</returns>
    /// <exception cref="InvalidOperationException">Invalid metadata and cannot parse it</exception>
    public Result<Dictionary<string, string>?, HttpError> Parse(string? metadata)
    {
        if (allowEmpty() && string.IsNullOrWhiteSpace(metadata))
        {
            return null;
        }
        else if (!allowEmpty() && string.IsNullOrWhiteSpace(metadata))
        {
            return HttpError.BadRequest("Must have Upload-Metadata header");
        }

        var keyValues = metadata!.Split(",");

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
                return HttpError.BadRequest("Invalid Upload-Metadata header values");
            }
        }

        var isValid = metadataValidator(result);
        if (!isValid)
        {
            return HttpError.BadRequest("Invalid Upload-Metadata");
        }

        return result;
    }
}

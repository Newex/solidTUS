using System;
using System.Collections.Generic;
using System.Linq;
using SolidTUS.Constants;
using SolidTUS.Contexts;
using SolidTUS.Models;
using SolidTUS.Models.Functional;
using SolidTUS.Parsers;
using SolidTUS.Validators;

namespace SolidTUS.ProtocolHandlers.ProtocolExtensions;

/// <summary>
/// Checksum request handler
/// </summary>
internal class ChecksumRequestHandler
{
    private readonly IEnumerable<IChecksumValidator> validators;

    /// <summary>
    /// Instantiate a new object <see cref="ChecksumRequestHandler"/>
    /// </summary>
    /// <param name="validators">The collection of checksum validators</param>
    public ChecksumRequestHandler(
        IEnumerable<IChecksumValidator> validators
    )
    {
        this.validators = validators;
    }

    /// <summary>
    /// Parse the checksum
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>An option tuple containing the algorithm name and the hash cipher</returns>
    public static Result<(string AlgorithmName, byte[] Cipher), HttpError> ParseChecksum(TusResult context)
    {
        var raw = context.RequestHeaders[TusHeaderNames.UploadChecksum];
        var result = ChecksumValueParser.DecodeCipher(raw!);
        if (result is not null && result.HasValue)
        {
            return result.Value;
        }
        else
        {
            return HttpError.BadRequest("Checksum must be base64 encoded");
        }
    }

    /// <summary>
    /// Set the checksum context for the request
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>The request context</returns>
    public Result<TusResult, HttpError> SetChecksum(TusResult context)
    {
        var hasChecksum = context.RequestHeaders.ContainsKey(TusHeaderNames.UploadChecksum);
        if (!hasChecksum)
        {
            return context;
        }

        var (isParsed, parse, error) = ParseChecksum(context);
        if (!isParsed)
        {
            return error;
        }

        var (algorithm, cipher) = parse!.Value;
        var validator = validators.SingleOrDefault(v => v.AlgorithmName.Equals(algorithm, StringComparison.OrdinalIgnoreCase));
        if (validator is null)
        {
            return HttpError.BadRequest("Checksum not supported");
        }

        context.ChecksumContext = new ChecksumContext
        {
            AlgorithmName = algorithm,
            Checksum = cipher,
            Validator = validator
        };

        return context;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using SolidTUS.Constants;
using SolidTUS.Contexts;
using SolidTUS.Extensions;
using SolidTUS.Models;
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
    public static (string AlgorithmName, byte[] Cipher)? ParseChecksum(TusResult context)
    {
        var raw = context.RequestHeaders[TusHeaderNames.UploadChecksum];
        return ChecksumValueParser.DecodeCipher(raw!);
    }

    /// <summary>
    /// Set the checksum context for the request
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>The request context</returns>
    public Result<TusResult> SetChecksum(TusResult context)
    {
        var hasChecksum = context.RequestHeaders.ContainsKey(TusHeaderNames.UploadChecksum);
        if (!hasChecksum)
        {
            return context.Wrap();
        }

        var checksum = ParseChecksum(context);
        if (checksum is null)
        {
            return HttpError.BadRequest("Invalid checksum request").Wrap();
        }

        var validator = validators.SingleOrDefault(v => v.AlgorithmName.Equals(checksum.Value.AlgorithmName, StringComparison.OrdinalIgnoreCase));
        if (validator is null)
        {
            return HttpError.BadRequest("Checksum not supported").Wrap();
        }

        context.ChecksumContext = new ChecksumContext
        {
            AlgorithmName = checksum.Value.AlgorithmName,
            Checksum = checksum.Value.Cipher,
            Validator = validator
        };

        return context.Wrap();
    }
}

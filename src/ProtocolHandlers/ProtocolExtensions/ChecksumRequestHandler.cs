using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using SolidTUS.Constants;
using SolidTUS.Models;
using SolidTUS.Parsers;
using SolidTUS.Validators;

namespace SolidTUS.ProtocolHandlers.ProtocolExtensions;

/// <summary>
/// Checksum request handler
/// </summary>
public class ChecksumRequestHandler
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
    public static Option<(string AlgorithmName, byte[] Cipher)> ParseChecksum(RequestContext context)
    {
        var raw = context.RequestHeaders[TusHeaderNames.UploadChecksum];
        return Optional(ChecksumValueParser.DecodeCipher(raw));
    }

    /// <summary>
    /// Set the checksum context for the request
    /// </summary>
    /// <param name="context">The request context</param>
    /// <param name="checksum">The checksum</param>
    /// <returns>The request context</returns>
    public RequestContext SetChecksum(RequestContext context, (string AlgorithmName, byte[] Cipher) checksum)
    {
        var validator = Optional(validators.SingleOrDefault(v => v.AlgorithmName.Equals(checksum.AlgorithmName, StringComparison.OrdinalIgnoreCase)));
        return context with
        {
            ChecksumContext = new ChecksumContext
            {
                AlgorithmName = checksum.AlgorithmName,
                Checksum = checksum.Cipher,
                Validator = validator
            }
        };
    }
}

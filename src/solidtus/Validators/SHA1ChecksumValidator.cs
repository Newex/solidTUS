using System;
using System.IO.Pipelines;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SolidTUS.Validators;

/// <summary>
/// Default SHA1 checksum validator
/// </summary>
public class SHA1ChecksumValidator : IChecksumValidator
{
    /// <inheritdoc />
    public string AlgorithmName => "sha1";

    /// <inheritdoc />
    public async Task<bool> ValidateChecksumAsync(PipeReader file, byte[] checksum)
    {
#pragma warning disable CA5350 // Is not sensitive info
        var hasher = SHA1.Create();
#pragma warning restore CA5350 // Do Not Use Weak Cryptographic Algorithms
        var cipher = await hasher.ComputeHashAsync(file.AsStream());
        if (cipher is null)
        {
            return false;
        }

        if (cipher.Length != checksum.Length)
        {
            return false;
        }

        return cipher.SequenceEqual(checksum);
    }
}

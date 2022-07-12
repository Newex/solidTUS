using System;
using System.IO;
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
    public Task<bool> ValidateChecksumAsync(Stream file, byte[] checksum)
    {
        var hasher = SHA1.Create();
        var cipher = hasher.ComputeHash(file);
        if (cipher is null)
        {
            return Task.FromResult(false);
        }

        if (cipher.Length != checksum.Length)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(cipher.SequenceEqual(checksum));
    }
}

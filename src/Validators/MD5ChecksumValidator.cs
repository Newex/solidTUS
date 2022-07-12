using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SolidTUS.Validators;

/// <summary>
/// Default MD5 checksum validator
/// </summary>
public class MD5ChecksumValidator : IChecksumValidator
{
    /// <inheritdoc />
    public string AlgorithmName => "md5";

    /// <inheritdoc />
    public Task<bool> ValidateChecksumAsync(Stream file, byte[] checksum)
    {
        var hasher = MD5.Create();
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

using System;
using System.IO.Pipelines;
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
    public async Task<bool> ValidateChecksumAsync(PipeReader file, byte[] checksum)
    {
        var hasher = MD5.Create();
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

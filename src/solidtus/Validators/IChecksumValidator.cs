using System.IO.Pipelines;
using System.Threading.Tasks;

namespace SolidTUS.Validators;

/// <summary>
/// Checksum validator interface
/// </summary>
public interface IChecksumValidator
{
    /// <summary>
    /// Get the algorithm name. This name must match what the client has specified, e.g. "sha1"
    /// </summary>
    string AlgorithmName { get; }

    /// <summary>
    /// Validate a given checksum
    /// </summary>
    /// <remarks>
    /// The checksum validator is only ever called when a checksum exists
    /// </remarks>
    /// <param name="file">The stream of the recently uploaded part that must be validated</param>
    /// <param name="checksum">The expected checksum value</param>
    /// <returns>True if valid checksum otherwise false</returns>
    Task<bool> ValidateChecksumAsync(PipeReader file, byte[] checksum);
}

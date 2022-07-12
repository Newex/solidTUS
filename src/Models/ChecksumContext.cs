using LanguageExt;
using SolidTUS.Validators;

namespace SolidTUS.Models;

/// <summary>
/// A checksum context
/// </summary>
public record ChecksumContext
{
    /// <summary>
    /// Get the checksum algorithm name
    /// </summary>
    public string AlgorithmName { get; init; } = string.Empty;

    /// <summary>
    /// The given checksum value
    /// </summary>
    public byte[] Checksum { get; init; } = System.Array.Empty<byte>();

    /// <summary>
    /// The optional checksum validator
    /// </summary>
    public Option<IChecksumValidator> Validator { get; init; }
}

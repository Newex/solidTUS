using System.Collections.Generic;
using SolidTUS.Contexts;

namespace SolidTUS.Models;

/// <summary>
/// The tus info
/// </summary>
public sealed record class TusInfo
{
    /// <summary>
    /// Instantiate a new tus info
    /// </summary>
    /// <param name="metadata">The tus metadata</param>
    /// <param name="fileSize">The total file size</param>
    /// <param name="checksum">The file checksum</param>
    public TusInfo(
        IReadOnlyDictionary<string, string>? metadata,
        long? fileSize,
        ChecksumContext? checksum = null)
    {
        Metadata = metadata;
        FileSize = fileSize;
        Checksum = checksum;
    }

    /// <summary>
    /// Get the tus metadata
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; }

    /// <summary>
    /// The total file size, as given by the <c>Upload-Length</c> header.
    /// </summary>
    public long? FileSize { get; }

    /// <summary>
    /// The file checksum context
    /// </summary>
    public ChecksumContext? Checksum { get; }
}

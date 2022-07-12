using System.Collections.Generic;

namespace SolidTUS.Models;

/// <summary>
/// Represents an upload file info
/// </summary>
public record UploadFileInfo
{
    /// <summary>
    /// Get the upload file ID
    /// </summary>
    public string ID { get; init; } = string.Empty;

    /// <summary>
    /// Get the actual upload file name
    /// </summary>
    public string ActualFilename { get; init; } = string.Empty;

    /// <summary>
    /// Get the MIME type
    /// </summary>
    public string MimeType { get; init; } = string.Empty;

    /// <summary>
    /// Get the bytes that have been uploaded so-far
    /// </summary>
    public long ByteOffset { get; init; }

    /// <summary>
    /// Get the total upload file size
    /// </summary>
    /// <remarks>
    /// If unknown file size this will be null
    /// </remarks>
    public long? FileSize { get; init; }

    /// <summary>
    /// Get the parsed TUS metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// Get the original raw metadata
    /// </summary>
    public string? RawMetadata { get; init; }
}

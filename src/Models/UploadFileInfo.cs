using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SolidTUS.Models;

/// <summary>
/// Represents an upload file info
/// </summary>
public record UploadFileInfo
{
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
    public Dictionary<string, string> Metadata { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Get the original raw metadata
    /// </summary>
    public string? RawMetadata { get; init; }

    /// <summary>
    /// Get the file path for this file
    /// </summary>
    [JsonInclude]
    public string? FilePath { get; internal set; }
}

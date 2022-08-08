using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
    public ImmutableDictionary<string, string> Metadata { get; init; } = Enumerable.Empty<KeyValuePair<string, string>>().ToImmutableDictionary();

    /// <summary>
    /// Get the original raw metadata
    /// </summary>
    public string? RawMetadata { get; init; }

    /// <summary>
    /// Get the file directory path for this file
    /// </summary>
    /// <remarks>
    /// The directory path
    /// </remarks>
    [JsonInclude]
    public string FileDirectoryPath { get; internal set; } = string.Empty;
}

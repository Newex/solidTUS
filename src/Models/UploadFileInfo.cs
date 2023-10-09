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
    /// Get the file id
    /// </summary>
    [JsonInclude]
    public string FileId { get; internal set; } = string.Empty;

    /// <summary>
    /// Get the bytes that have been uploaded so-far
    /// </summary>
    [JsonInclude]
    public long ByteOffset { get; private set; }

    /// <summary>
    /// Get the total upload file size
    /// </summary>
    /// <remarks>
    /// If unknown file size this will be null
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonInclude]
    public long? FileSize { get; init; }

    /// <summary>
    /// Get if the upload file is a partial file
    /// </summary>
    [JsonInclude]
    public bool IsPartial { get; internal set; }

    /// <summary>
    /// The <c>Upload-Concat</c> header value.
    /// </summary>
    /// <remarks>
    /// Only set when the value is finalized and merged.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonInclude]
    public string? ConcatHeaderFinal { get; internal set; }

    /// <summary>
    /// Get the parsed TUS metadata
    /// </summary>
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string>? Metadata { get; internal set; }

    /// <summary>
    /// Get the original raw metadata
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonInclude]
    public string? RawMetadata { get; internal set; }

    /// <summary>
    /// Get the filename of the uploaded file as it is on the disk
    /// </summary>
    /// <remarks>
    /// This filename is different from the given actual filename.
    /// It is recommended to change filename when saving uploaded file to avoid any exploits and filename collisions
    /// </remarks>
    [JsonInclude]
    public string OnDiskFilename { get; internal set; } = string.Empty;

    /// <summary>
    /// Get the directory path for where the file is stored on disk
    /// </summary>
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OnDiskDirectoryPath { get; internal set; }

    /// <summary>
    /// Get the specific date-time for the upload expiration.
    /// </summary>
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? ExpirationDate { get; internal set; }

    /// <summary>
    /// The created date of the upload and metadata
    /// </summary>
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? CreatedDate { get; internal set; }

    /// <summary>
    /// Last time this upload was updated
    /// </summary>
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? LastUpdatedDate { get; internal set; }

    /// <summary>
    /// If the upload has finished
    /// </summary>
    [JsonIgnore]
    public bool Done => FileSize.HasValue && FileSize.Value == ByteOffset;

    /// <summary>
    /// Add amount of bytes to the offset
    /// </summary>
    /// <param name="bytes">The amount of bytes added</param>
    public void AddBytes(long bytes)
    {
        ByteOffset += bytes;
    }
}

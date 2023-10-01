using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using SolidTUS.Attributes;

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
    public long ByteOffset { get; internal set; }

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
    /// Get the url path for the partial upload
    /// </summary>
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PartialUrlPath { get; internal set; }

    /// <summary>
    /// Get the parsed TUS metadata
    /// </summary>
    [JsonReadOnlyDictionary]
    [JsonInclude]
    public ReadOnlyDictionary<string, string> Metadata { get; internal set; } = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

    /// <summary>
    /// Get the original raw metadata
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonInclude]
    public string? RawMetadata { get; internal set; }

    /// <summary>
    /// Get the file directory path for this file
    /// </summary>
    /// <remarks>
    /// The directory path
    /// </remarks>
    [JsonInclude]
    public string FileDirectoryPath { get; internal set; } = string.Empty;

    /// <summary>
    /// Get the filename as it is on the disk
    /// </summary>
    /// <remarks>
    /// This filename is different from the given actual filename.
    /// It is recommended to change filename when saving uploaded file to avoid any exploits and filename collisions
    /// </remarks>
    [JsonInclude]
    public string OnDiskFilename { get; internal set; } = string.Empty;

    /// <summary>
    /// Get the specific expiration strategy for this related upload.
    /// </summary>
    /// <remarks>
    /// If null this means that the global <see cref="ExpirationStrategy"/> is used.
    /// </remarks>
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ExpirationStrategy? ExpirationStrategy { get; internal set; }

    /// <summary>
    /// Get the specific date-time for the upload expiration.
    /// </summary>
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? ExpirationDate { get; internal set; }

    /// <summary>
    /// Get the specific interval for this upload.
    /// </summary>
    /// <remarks>
    /// If null, the global settings is used, if applicable.
    /// </remarks>
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TimeSpan? Interval { get; internal set; }

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

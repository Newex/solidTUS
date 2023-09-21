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
    [JsonReadOnlyDictionary]
    public ReadOnlyDictionary<string, string> Metadata { get; init; } = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

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
    public ExpirationStrategy? ExpirationStrategy { get; internal set; }

    /// <summary>
    /// Get the specific date-time for the upload expiration.
    /// </summary>
    public DateTimeOffset? ExpirationDate { get; internal set; }

    /// <summary>
    /// Get the specific interval for this upload.
    /// </summary>
    /// <remarks>
    /// If null, the global settings is used, if applicable.
    /// </remarks>
    public TimeSpan? Interval { get; internal set; }

    /// <summary>
    /// The created date of the upload and metadata
    /// </summary>
    public DateTimeOffset CreatedDate { get; internal set; }
}

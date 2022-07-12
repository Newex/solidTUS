using System;

namespace SolidTUS.Options;

/// <summary>
/// Options for the default file storage upload handler
/// </summary>
public record FileStorageOptions
{
    /// <summary>
    /// The directory where to save the uploads
    /// </summary>
    public string DirectoryPath { get; set; } = Environment.CurrentDirectory;

    internal const string FileStorageSection = "FileOptions";
}

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

    /// <summary>
    /// The directory where to save the meta files
    /// </summary>
    public string MetaDirectoryPath { get; set; } = Environment.CurrentDirectory;
}

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

    /// <summary>
    /// The maximum number of milliseconds to wait to perform an IO that requires obtaining a lock.
    /// </summary>
    /// <remarks>
    /// To wait indefinitely set to negative 1 (-1).
    /// Default is 5000 ms (5 sec).
    /// </remarks>
    public int MaxWaitInMilliseconds = 5_000;
}

namespace SolidTUS.Models;

/// <summary>
/// Determine which mode a request is beginning
/// </summary>
public enum PartialMode
{
    /// <summary>
    /// Default mode, single file upload.
    /// </summary>
    None,

    /// <summary>
    /// Starting new partial file
    /// </summary>
    /// <remarks>
    /// This corresponds to <c>Upload-Concat: partial</c> header.
    /// </remarks>
    Partial,

    /// <summary>
    /// Merging partial files
    /// </summary>
    /// <remarks>
    /// This corresponds to <c>Upload-Concat: final</c> header.
    /// </remarks>
    Final,
}

using System;

namespace SolidTUS.Constants;

/// <summary>
/// TUS header values
/// </summary>
public static class TusHeaderValues
{
    /// <summary>
    /// The content-type value for PATCH request
    /// </summary>
    public const string PatchContentType = "application/offset+octet-stream";

    /// <summary>
    /// Get a comma separated list of supported TUS-protocol versions
    /// </summary>
    public const string TusServerVersions = "1.0.0";

    /// <summary>
    /// Get the actual TUS-protocol used in response
    /// </summary>
    public const string TusPreferredVersion = "1.0.0";

    /// <summary>
    /// Get the actual supported protocol extensions
    /// </summary>
    public const string TusSupportedExtensions = "creation,creation-with-upload,checksum";
}

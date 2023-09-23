using System;

namespace SolidTUS.Constants;

/// <summary>
/// TUS header keys
/// </summary>
public static class TusHeaderNames
{
    /// <summary>
    /// This header should be included in every request except OPTIONS.
    /// The value must correspond to the server TUS version
    /// </summary>
    public const string Resumable = "Tus-Resumable";

    /// <summary>
    /// Represents a TUS-version.
    /// The value returns a comma separated list of versions that the server supports.
    /// </summary>
    public const string Version = "Tus-Version";

    /// <summary>
    /// Indicates a byte offset within a resource
    /// </summary>
    public const string UploadOffset = "Upload-Offset";

    /// <summary>
    /// Indicates the total upload size given beforehand
    /// </summary>
    public const string UploadLength = "Upload-Length";

    /// <summary>
    /// Indicates that the total upload size is unknown
    /// </summary>
    public const string UploadDeferLength = "Upload-Defer-Length";

    /// <summary>
    /// Custom TUS upload metadata
    /// </summary>
    public const string UploadMetadata = "Upload-Metadata";

    /// <summary>
    /// The maximum file size allowed by the server
    /// </summary>
    public const string MaxSize = "Tus-Max-Size";

    /// <summary>
    /// The supported protocol extensions by the server
    /// </summary>
    public const string Extension = "Tus-Extension";

    /// <summary>
    /// The client upload checksum algorithm and cipher
    /// </summary>
    public const string UploadChecksum = "Upload-Checksum";

    /// <summary>
    /// Get the checksum algorithm header value
    /// </summary>
    public const string ChecksumAlgorithm = "Tus-Checksum-Algorithm";

    /// <summary>
    /// Get the http method override header value
    /// </summary>
    public const string HttpMethodOverride = "X-HTTP-Method-Override";

    /// <summary>
    /// Get the expiration header value
    /// </summary>
    public const string Expiration = "Upload-Expires";
}

namespace SolidTUS.Constants;

/// <summary>
/// Default attribute names for the various endpoints
/// </summary>
public static class EndpointNames
{
    /// <summary>
    /// Get default name for the tus-creation endpoint.
    /// </summary>
    public const string CreationEpoint = "SolidTusCreationEndpoint";

    /// <summary>
    /// Get default name for the tus-upload endpoint
    /// </summary>
    public const string UploadEndpoint = "SolidTusUploadEndpoint";

    /// <summary>
    /// Get default name for the tus-termination endpoint
    /// </summary>
    public const string TerminationEndpoint = "SolidTusTerminateEndpoint";

    /// <summary>
    /// Get the default route name for the parallel endpoint
    /// </summary>
    public const string ParallelEndpoint = "SolidTusParallelEndpoint";
}

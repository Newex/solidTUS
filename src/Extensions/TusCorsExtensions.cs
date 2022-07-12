using Microsoft.AspNetCore.Cors.Infrastructure;
using SolidTUS.Constants;

namespace SolidTUS.Extensions;

/// <summary>
/// Extension helpers for CORS policy
/// </summary>
public static class TusCorsExtensions
{
    /// <summary>
    /// Adds a collection of TUS headers to the policy, <see cref="TusHeaderNames"/> constants
    /// </summary>
    /// <param name="policy">The cors policy builder</param>
    /// <returns>A cors policy builder</returns>
    public static CorsPolicyBuilder WithExposedTusHeaders(this CorsPolicyBuilder policy)
    {
        return policy.WithExposedHeaders(new[]
        {
            "Location",
            TusHeaderNames.Resumable,
            TusHeaderNames.Version,
            TusHeaderNames.Extension,
            TusHeaderNames.MaxSize,
            TusHeaderNames.ChecksumAlgorithm,
            TusHeaderNames.UploadLength,
            TusHeaderNames.UploadDeferLength,
            TusHeaderNames.UploadOffset,
            TusHeaderNames.UploadMetadata,
            TusHeaderNames.UploadChecksum,
            TusHeaderNames.HttpMethodOverride
        });
    }

}

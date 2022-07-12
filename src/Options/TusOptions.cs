using System;
using System.Collections.Generic;

namespace SolidTUS.Options;

/// <summary>
/// TUS options
/// </summary>
public record TusOptions
{
    /// <summary>
    /// Get or set the metadata validator
    /// </summary>
    /// <remarks>
    /// Default validator returns true on any input
    /// </remarks>
    public Func<Dictionary<string, string>, bool> MetadataValidator { get; set; } = (_) => true;

    /// <summary>
    /// Get or set the maximum TUS upload size
    /// </summary>
    public long? MaxSize { get; set; }
}

using System;
using System.Collections.Generic;

using SolidTUS.Models;

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

    /// <summary>
    /// Get or set the global expiration strategy.
    /// </summary>
    /// <remarks>
    /// Default <see cref="Models.ExpirationStrategy"/> is <see cref="ExpirationStrategy.Never"/>
    /// </remarks>
    public ExpirationStrategy ExpirationStrategy { get; set; } = ExpirationStrategy.Never;

    /// <summary>
    /// Get or set the global sliding expiration interval.
    /// </summary>
    /// <remarks>
    /// Only used if <see cref="Models.ExpirationStrategy"/> is set to <see cref="ExpirationStrategy.SlidingExpiration"/> or <see cref="ExpirationStrategy.SlideAfterAbsoluteExpiration"/>.
    /// <para>
    /// The default interval is 10 minutes.
    /// </para>
    /// </remarks>
    public TimeSpan SlidingInterval { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Get or set the global absolute interval.
    /// </summary>
    /// <remarks>
    /// Only used if <see cref="Models.ExpirationStrategy"/> is set to <see cref="ExpirationStrategy.AbsoluteExpiration"/> or <see cref="ExpirationStrategy.SlideAfterAbsoluteExpiration"/>.
    /// <para>
    /// The default interval is 1 hour.
    /// </para>
    /// </remarks>
    public TimeSpan AbsoluteInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Allow an incomplete expired upload to continue if still exists.
    /// </summary>
    /// <remarks>
    /// Default is false.
    /// </remarks>
    public bool AllowExpiredUploadsToContinue { get; set; }

    /// <summary>
    /// Set the interval that the expiration job runner should start scanning for expired uploads.
    /// </summary>
    /// <remarks>
    /// Default interval is 1 day.
    /// </remarks>
    public TimeSpan ExpirationJobRunnerInterval { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Get the configuration section name
    /// </summary>
    internal const string TusConfigurationSection = "SolidTUS";
}

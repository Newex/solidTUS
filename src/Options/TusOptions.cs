using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using SolidTUS.Models;

namespace SolidTUS.Options;

/// <summary>
/// TUS options
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public record TusOptions
{
    /// <summary>
    /// Get or set the metadata validator
    /// </summary>
    /// <remarks>
    /// Default validator returns true on any input
    /// </remarks>
    public Func<IReadOnlyDictionary<string, string>, bool> MetadataValidator { get; set; } = (_) => true;

    /// <summary>
    /// Get or set if the parallel / partial uploads should require to conform to the metadata validator specifications.
    /// </summary>
    /// <remarks>
    /// Beware setting this to true might unnecessarily invalidate parallel uploads.
    /// </remarks>
    public bool ValidateMetadataForParallelUploads { get; set; }

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
    /// The default interval is 1 day.
    /// </para>
    /// </remarks>
    public TimeSpan SlidingInterval { get; set; } = TimeSpan.FromDays(1);

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
    /// This will signal the TUS capability when a client sends an OPTION discovery.
    /// <para>
    /// By adding the TUS-extension header with "termination".
    /// </para>
    /// </summary>
    /// <remarks>
    /// You must implement termination yourself. See example or documentation for more info.
    /// </remarks>
    public bool HasTermination { get; set; }

    /// <summary>
    /// If true, partials files will be deleted when finished merging into final file.
    /// Otherwise the partials files will be kept.
    /// </summary>
    /// <remarks>
    /// Default true.
    /// </remarks>
    public bool DeletePartialFilesOnMerge { get; set; } = true;

    /// <summary>
    /// Get the configuration section name
    /// </summary>
    internal const string TusConfigurationSection = "SolidTUS";
}

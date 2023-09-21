namespace SolidTUS.Models;

/// <summary>
/// Expiration strategy
/// </summary>
public enum ExpirationStrategy
{
    /// <summary>
    /// Never expire uploads and keep them indefinitely
    /// </summary>
    Never,

    /// <summary>
    /// Strategy that refreshes the expiration deadline upon receiving upload activity
    /// </summary>
    SlidingExpiration,

    /// <summary>
    /// Strategy that expires the upload at a specific time
    /// </summary>
    AbsoluteExpiration,

    /// <summary>
    /// Strategy that if the absolute expiration deadline is reached while still uploading the strategy will switch to <see cref="SlidingExpiration"/>.
    /// <para>
    /// If the absolute expiration is reached with no activity the upload expires.
    /// </para>
    /// </summary>
    SlideAfterAbsoluteExpiration,
}

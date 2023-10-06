using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using SolidTUS.Constants;
using SolidTUS.Contexts;
using SolidTUS.Extensions;
using SolidTUS.Handlers;
using SolidTUS.Models;
using SolidTUS.Options;

namespace SolidTUS.ProtocolHandlers.ProtocolExtensions;

/// <summary>
/// Expiration TUS extension request handler
/// </summary>
internal class ExpirationRequestHandler
{
    private readonly ExpirationStrategy expirationStrategy;
    private readonly TimeSpan slidingInterval;
    private readonly TimeSpan absoluteInterval;
    private readonly ISystemClock clock;
    private readonly IExpiredUploadHandler expiredUploadHandler;

    private readonly bool allowExpiredUploads;

    /// <summary>
    /// Instantiate a new <see cref="ExpirationRequestHandler"/>
    /// </summary>
    /// <param name="clock">The system clock provider</param>
    /// <param name="expiredUploadHandler">The expired upload handler</param>
    /// <param name="options">The TUS options</param>
    public ExpirationRequestHandler(
        ISystemClock clock,
        IExpiredUploadHandler expiredUploadHandler,
        IOptions<TusOptions> options
    )
    {
        expirationStrategy = options.Value.ExpirationStrategy;
        slidingInterval = options.Value.SlidingInterval;
        absoluteInterval = options.Value.AbsoluteInterval;
        allowExpiredUploads = options.Value.AllowExpiredUploadsToContinue;
        this.clock = clock;
        this.expiredUploadHandler = expiredUploadHandler;
    }

    /// <summary>
    /// Set the expiration header if resource has expiration date
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>A request context with expiration headers</returns>
    public TusResult SetExpiration(TusResult context)
    {
        if (context.UploadFileInfo is null
            || context.UploadFileInfo.ExpirationStrategy == ExpirationStrategy.Never
            || expirationStrategy == ExpirationStrategy.Never
            && context.UploadFileInfo.ExpirationStrategy is null)
        {
            return context;
        }

        if (context.UploadFileInfo.ExpirationDate is null)
        {
            return context;
        }

        // Convert the end date to RFC 7231
        var time = ToRFC7231(context.UploadFileInfo.ExpirationDate.Value);

        // Overwrite if exists
        context.ResponseHeaders[TusHeaderNames.Expiration] = time;
        return context;
    }

    public async Task<Result<TusResult>> CheckExpirationAsync(TusResult context, CancellationToken cancellationToken)
    {
        if (context.UploadFileInfo?.ExpirationDate.HasValue ?? false)
        {
            var now = clock.UtcNow;
            var expired = now > context.UploadFileInfo.ExpirationDate.Value;
            if (expired)
            {
                await expiredUploadHandler.ExpiredUploadAsync(context.UploadFileInfo, cancellationToken);
                return HttpError.Gone().Wrap();
            }
        }

        return context.Wrap();
    }

    /// <summary>
    /// Calculate the end date if it exists otherwise if not then it will be null
    /// </summary>
    /// <param name="strategy">The expiration strategy</param>
    /// <param name="current">The current time</param>
    /// <param name="created">The created time</param>
    /// <param name="updated">The updated time</param>
    /// <param name="absoluteInterval">The interval for absolute</param>
    /// <param name="slidingInterval">The interval for sliding</param>
    /// <returns>A datetime offset or null</returns>
    /// <exception cref="UnreachableException">Thrown if missing strategy enum</exception>
    public static DateTimeOffset? CalculateExpiration(
        ExpirationStrategy strategy,
        DateTimeOffset current,
        DateTimeOffset created,
        DateTimeOffset? updated,
        TimeSpan absoluteInterval,
        TimeSpan slidingInterval)
    {
        DateTimeOffset? end = strategy switch
        {
            ExpirationStrategy.Never => null,
            ExpirationStrategy.SlidingExpiration => Sliding(created, updated, slidingInterval),
            ExpirationStrategy.AbsoluteExpiration => Absolute(created, absoluteInterval),
            ExpirationStrategy.SlideAfterAbsoluteExpiration => SlideAfterAbsolute(current, created, updated, absoluteInterval, slidingInterval),
            _ => throw new UnreachableException()
        };

        return end;
    }

    private static DateTimeOffset Sliding(DateTimeOffset created, DateTimeOffset? updated, TimeSpan interval)
    {
        if (updated is not null)
        {
            return updated.Value.Add(interval);
        }

        return created.Add(interval);
    }

    private static DateTimeOffset Absolute(DateTimeOffset created, TimeSpan interval)
    {
        return created.Add(interval);
    }

    private static DateTimeOffset SlideAfterAbsolute(DateTimeOffset now, DateTimeOffset created, DateTimeOffset? updated, TimeSpan absolute, TimeSpan slide)
    {
        var absoluteDeadline = Absolute(created, absolute);
        var isPastDeadline = now > absoluteDeadline;

        if (isPastDeadline)
        {
            // We slide
            return Sliding(created, updated, slide);
        }

        return Absolute(created, absolute);
    }

    private static string ToRFC7231(DateTimeOffset time)
    {
        var utc = time.ToUniversalTime();
        var rfc = utc.ToString(@"ddd, dd MMM yyyy HH:mm:ss G\MT", CultureInfo.CreateSpecificCulture("en"));
        return rfc;
    }
}

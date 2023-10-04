using System;
using System.Diagnostics;
using System.Globalization;
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
public class ExpirationRequestHandler
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
    /// Check the upload info expiration
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>A request context or an error</returns>
    /// <exception cref="UnreachableException">Thrown if missing strategy enumeration</exception>
    public async Task<Result<RequestContext>> CheckExpirationAsync(RequestContext context)
    {
        var info = context.UploadFileInfo;
        if (info.Done)
        {
            // File uploaded - can only expire unfinished uploads
            return context.Wrap();
        }

        var strategy = info.ExpirationStrategy ?? expirationStrategy;
        var lastTime = info.LastUpdatedDate ?? info.CreatedDate;
        if (lastTime is null)
        {
            // Could not find updated date or created date
            return HttpError.InternalServerError().Request();
        }

        DateTimeOffset? deadline = strategy switch
        {
            ExpirationStrategy.Never => null,
            ExpirationStrategy.SlidingExpiration => Sliding(lastTime.Value, info),
            ExpirationStrategy.AbsoluteExpiration => Absolute(info),
            ExpirationStrategy.SlideAfterAbsoluteExpiration => SlideAfterAbsolute(lastTime.Value, info),
            _ => throw new UnreachableException()
        };

        if (deadline is null)
        {
            return context.Wrap();
        }

        var now = clock.UtcNow;
        var expired = now > deadline.Value;
        if (expired && !allowExpiredUploads)
        {
            await expiredUploadHandler.ExpiredUploadAsync(info, context.CancellationToken);
            return HttpError.Gone("Upload expired").Request();
        }

        return context.Wrap();
    }

    /// <summary>
    /// Set the expiration header if resource has expiration date
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>A request context with expiration headers</returns>
    public void SetExpiration(ResponseContext context)
    {
        if (context.UploadFileInfo is null
            || context.UploadFileInfo.ExpirationStrategy == ExpirationStrategy.Never
            || expirationStrategy == ExpirationStrategy.Never
            && context.UploadFileInfo.ExpirationStrategy is null)
        {
            return;
        }

        if (context.UploadFileInfo.ExpirationDate is null)
        {
            return;
        }

        // Convert the end date to RFC 7231
        var time = ToRFC7231(context.UploadFileInfo.ExpirationDate.Value);

        // Overwrite if exists
        context.ResponseHeaders[TusHeaderNames.Expiration] = time;
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

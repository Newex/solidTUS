using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using SolidTUS.Constants;
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
        var strategy = info.ExpirationStrategy ?? expirationStrategy;
        var lastTime = info.LastUpdatedDate ?? info.CreatedDate;
        if (lastTime is null)
        {
            // Could not find updated date or created date
            return HttpError.InternalServerError().Wrap();
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
            return HttpError.Gone("Upload expired").Wrap();
        }

        return context.Wrap();
    }

    /// <summary>
    /// Set the expiration header if TUS options is set
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>A request context with expiration headers</returns>
    /// <exception cref="UnreachableException">Thrown if expiration strategy is not in the enumeration</exception>
    public RequestContext SetExpiration(RequestContext context)
    {
        if (context.UploadFileInfo.ExpirationStrategy == ExpirationStrategy.Never
            || expirationStrategy == ExpirationStrategy.Never
            && context.UploadFileInfo.ExpirationStrategy is null)
        {
            // No expiration
            return context;
        }

        // Assume we have accepted the incoming upload = within the expiration time.
        // Assume CreatedDate is set.
        var now = clock.UtcNow;
        var strategy = context.UploadFileInfo.ExpirationStrategy ?? expirationStrategy;
        var end = strategy switch
        {
            ExpirationStrategy.SlidingExpiration => Sliding(now, context.UploadFileInfo),
            ExpirationStrategy.AbsoluteExpiration => Absolute(context.UploadFileInfo),
            ExpirationStrategy.SlideAfterAbsoluteExpiration => SlideAfterAbsolute(now, context.UploadFileInfo),
            _ => throw new UnreachableException()
        };

        context.UploadFileInfo.ExpirationDate = end;

        // Convert the end date to RFC 7231
        var time = ToRFC7231(end);

        // Overwrite if exists
        context.ResponseHeaders[TusHeaderNames.Expiration] = time;
        return context;
    }

    private DateTimeOffset Sliding(DateTimeOffset from, UploadFileInfo info)
    {
        var interval = info.Interval ?? slidingInterval;
        return from.Add(interval);
    }

    private DateTimeOffset Absolute(UploadFileInfo info)
    {
        var interval = info.Interval ?? absoluteInterval;
        var deadline = info.CreatedDate?.Add(interval);
        return deadline.GetValueOrDefault();
    }

    private DateTimeOffset SlideAfterAbsolute(DateTimeOffset from, UploadFileInfo info)
    {
        var absoluteDeadline = info.CreatedDate?.Add(absoluteInterval);
        var isPastDeadline = from > absoluteDeadline;

        if (isPastDeadline)
        {
            // We slide
            return Sliding(from, info);
        }

        return Absolute(info);
    }

    private static string ToRFC7231(DateTimeOffset time)
    {
        var utc = time.ToUniversalTime();
        var rfc = utc.ToString(@"ddd, dd MMM yyyy HH:mm:ss G\MT", CultureInfo.CreateSpecificCulture("en"));
        return rfc;
    }
}

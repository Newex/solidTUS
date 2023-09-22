using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using SolidTUS.Constants;
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="clock"></param>
    /// <param name="options"></param>
    public ExpirationRequestHandler(
        ISystemClock clock,
        IOptions<TusOptions> options
    )
    {
        expirationStrategy = options.Value.ExpirationStrategy;
        slidingInterval = options.Value.SlidingInterval;
        absoluteInterval = options.Value.AbsoluteInterval;
        this.clock = clock;

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

        // Convert the end date to RFC 7231
        var time = ToRFC7231(end);

        // Overwrite if exists
        context.ResponseHeaders[TusHeaderNames.Expiration] = time;
        return context;
    }

    private DateTimeOffset Sliding(DateTimeOffset current, UploadFileInfo info)
    {
        var interval = info.Interval ?? slidingInterval;
        return current.Add(interval);
    }

    private DateTimeOffset Absolute(UploadFileInfo info)
    {
        var interval = info.Interval ?? absoluteInterval;
        var deadline = info.CreatedDate?.Add(interval);
        return deadline.GetValueOrDefault();
    }

    private DateTimeOffset SlideAfterAbsolute(DateTimeOffset current, UploadFileInfo info)
    {
        var absoluteDeadline = info.CreatedDate?.Add(absoluteInterval);
        var isPastDeadline = current > absoluteDeadline;

        if (isPastDeadline)
        {
            // We slide
            return Sliding(current, info);
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

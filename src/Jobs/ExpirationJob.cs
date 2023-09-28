using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SolidTUS.Handlers;
using SolidTUS.Options;

namespace SolidTUS.Jobs;

/// <summary>
/// A background service expiration job runner that periodically scans for expired uploads.
/// </summary>
public class ExpirationJob : BackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<ExpirationJob> logger;
    private readonly PeriodicTimer timer;
    private readonly TimeSpan interval;

    /// <summary>
    /// Instantiate a new <see cref="ExpirationJob"/>
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="options">The TUS options</param>
    /// <param name="logger">The optional logger</param>
    public ExpirationJob(
        IServiceProvider serviceProvider,
        IOptions<TusOptions> options,
        ILogger<ExpirationJob>? logger = null
    )
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger ?? NullLogger<ExpirationJob>.Instance;

        interval = options.Value.ExpirationJobRunnerInterval;
        timer = new PeriodicTimer(interval);
    }

    /// <summary>
    /// Start the periodic scanning of expired uploads
    /// </summary>
    /// <param name="stoppingToken">The stopping token</param>
    /// <returns>An awaitable task</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting expired uploads scanner job, every {Interval}", interval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync())
        {
            await Scan(stoppingToken);
        }
    }

    private async Task Scan(CancellationToken cancellationToken)
    {
        logger.LogInformation("Scanning for expired uploads...");
        using var scope = serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IExpiredUploadHandler>();

        await handler.StartScanForExpiredUploadsAsync(cancellationToken);
    }
}

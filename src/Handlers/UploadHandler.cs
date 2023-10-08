using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using SolidTUS.Contexts;
using SolidTUS.Extensions;
using SolidTUS.Models;
using SolidTUS.Options;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;

namespace SolidTUS.Handlers;

internal class UploadHandler
{
    private readonly ExpirationStrategy expirationStrategy;
    private readonly TimeSpan absoluteInterval;
    private readonly TimeSpan slidingInterval;
    private readonly ISystemClock clock;
    private readonly IUploadMetaHandler uploadMetaHandler;
    private readonly IUploadStorageHandler uploadStorageHandler;
    private readonly IExpiredUploadHandler expiredUploadHandler;

    public UploadHandler(
        ISystemClock clock,
        IOptions<TusOptions> options,
        IUploadMetaHandler uploadMetaHandler,
        IUploadStorageHandler uploadStorageHandler,
        IExpiredUploadHandler expiredUploadHandler
    )
    {
        expirationStrategy = options.Value.ExpirationStrategy;
        absoluteInterval = options.Value.AbsoluteInterval;
        slidingInterval = options.Value.SlidingInterval;
        this.clock = clock;

        this.uploadMetaHandler = uploadMetaHandler;
        this.uploadStorageHandler = uploadStorageHandler;
        this.expiredUploadHandler = expiredUploadHandler;
    }

    public async Task<Result<TusResult>> HandleUploadAsync(PipeReader reader, TusUploadContext uploadContext, TusResult tusResult, CancellationToken cancellationToken)
    {
        var info = tusResult.UploadFileInfo;
        if (info is null)
        {
            throw new InvalidOperationException("Missing upload info");
        }

        if (info.FileId != uploadContext.FileId)
        {
            throw new InvalidOperationException("File id does not match the file id given");
        }

        try
        {
            await uploadStorageHandler.OnPartialUploadAsync(reader, info, tusResult.ChecksumContext, cancellationToken);
        }
        finally
        {
            // calculate expiration
            var now = clock.UtcNow;
            var expirationDate = ExpirationRequestHandler.CalculateExpiration(info.ExpirationStrategy ?? expirationStrategy, now, info.CreatedDate ?? now, info.LastUpdatedDate, absoluteInterval, slidingInterval);
            info.ExpirationDate = expirationDate;
            await uploadMetaHandler.UpdateResourceAsync(info, cancellationToken);
        }

        if (info.Done)
        {
            if (uploadContext.UploadFinishedCallback is not null)
            {
                await uploadContext.UploadFinishedCallback(info);
            }

            return tusResult.Wrap();
        }

        return HttpError.InternalServerError().Wrap();
    }
}

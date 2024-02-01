using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using SolidTUS.Models;
using SolidTUS.Options;

namespace SolidTUS.Handlers;

/// <summary>
/// File expiration upload handler
/// </summary>
public class FileExpiredUploadHandler : IExpiredUploadHandler
{
    private readonly ISystemClock clock;

    private readonly IUploadMetaHandler uploadMetaHandler;
    private readonly string directory;

    /// <summary>
    /// Instantiate a new object of <see cref="FileExpiredUploadHandler"/>
    /// </summary>
    /// <param name="clock">The system clock</param>
    /// <param name="uploadMetaHandler">The upload meta handler</param>
    /// <param name="options"></param>
    public FileExpiredUploadHandler(
        ISystemClock clock,
        IUploadMetaHandler uploadMetaHandler,
        IOptions<FileStorageOptions> options
     )
    {
        this.clock = clock;
        this.uploadMetaHandler = uploadMetaHandler;
        directory = options.Value.DirectoryPath;
    }

    /// <inheritdoc />
    public async Task ExpiredUploadAsync(UploadFileInfo uploadFileInfo, CancellationToken cancellationToken)
    {
        await uploadMetaHandler.DeleteUploadFileInfoAsync(uploadFileInfo, cancellationToken);
        var file = Path.Combine(directory, uploadFileInfo.OnDiskFilename);
        File.Delete(file);
    }

    /// <inheritdoc />
    public async Task StartScanForExpiredUploadsAsync(CancellationToken cancellationToken)
    {
        await foreach (var info in uploadMetaHandler.GetAllResourcesAsync())
        {
            if (info.ExpirationDate.HasValue && !info.Done)
            {
                var now = clock.UtcNow;
                var expired = now > info.ExpirationDate.Value;
                if (expired)
                {
                    await ExpiredUploadAsync(info, cancellationToken);
                }
            }
        }
    }
}

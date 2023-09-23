using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using SolidTUS.Models;

namespace SolidTUS.Handlers;

/// <summary>
/// File expiration upload handler
/// </summary>
public class FileExpiredUploadHandler : IExpiredUploadHandler
{
    private readonly ISystemClock clock;

    private readonly IUploadMetaHandler uploadMetaHandler;

    /// <summary>
    /// Instantiate a new object of <see cref="FileExpiredUploadHandler"/>
    /// </summary>
    /// <param name="clock">The system clock</param>
    /// <param name="uploadMetaHandler">The upload meta handler</param>
    public FileExpiredUploadHandler(
        ISystemClock clock,
        IUploadMetaHandler uploadMetaHandler
     )
    {
        this.clock = clock;

        this.uploadMetaHandler = uploadMetaHandler;
    }

    /// <inheritdoc />
    public async Task ExpiredUploadAsync(UploadFileInfo uploadFileInfo)
    {
        await uploadMetaHandler.DeleteUploadFileInfoAsync(uploadFileInfo.FileId, CancellationToken.None);
        var file = Path.Combine(uploadFileInfo.FileDirectoryPath, uploadFileInfo.OnDiskFilename);
        File.Delete(file);
    }

    /// <inheritdoc />
    public async Task StartScanForExpiredUploadsAsync()
    {
        await foreach (var info in uploadMetaHandler.GetAllResourcesAsync())
        {
            if (info.ExpirationDate.HasValue)
            {
                var now = clock.UtcNow;
                var expired = now > info.ExpirationDate.Value;
                if (expired)
                {
                    await ExpiredUploadAsync(info);
                }
            }
        }
    }
}

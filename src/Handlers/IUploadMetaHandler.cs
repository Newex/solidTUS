using System.Threading;
using System.Threading.Tasks;
using SolidTUS.Models;

namespace SolidTUS.Handlers;

/// <summary>
/// Upload file info meta handler.
/// Handles information regarding file size, byte offsets etc.
/// </summary>
/// <seealso cref="UploadFileInfo"/>
public interface IUploadMetaHandler
{
    /// <summary>
    /// Get the upload file info
    /// </summary>
    /// <param name="fileId">The file Id</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>An upload file info or null</returns>
    ValueTask<UploadFileInfo?> GetUploadFileInfoAsync(string fileId, CancellationToken cancellationToken);

    /// <summary>
    /// Set the file size in <see cref="UploadFileInfo.FileSize"/>
    /// </summary>
    /// <param name="fileId">The file Id</param>
    /// <param name="totalFileSize">The total file size</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>True if set otherwise false</returns>
    ValueTask<bool> SetFileSizeAsync(string fileId, long totalFileSize, CancellationToken cancellationToken);

    /// <summary>
    /// Create file info resource
    /// </summary>
    /// <param name="fileId">The file Id</param>
    /// <param name="fileInfo">The file info</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>True if created otherwise false</returns>
    ValueTask<bool> CreateResourceAsync(string fileId, UploadFileInfo fileInfo, CancellationToken cancellationToken);

    /// <summary>
    /// Set the total uploaded size for the file
    /// </summary>
    /// <param name="fileId">The file Id</param>
    /// <param name="totalBytes">The total bytes</param>
    /// <returns>True if set otherwise false</returns>
    ValueTask<bool> SetTotalUploadedBytesAsync(string fileId, long totalBytes);
}

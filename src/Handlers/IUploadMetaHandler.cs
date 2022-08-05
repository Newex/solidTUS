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
    Task<UploadFileInfo?> GetUploadFileInfoAsync(string fileId, CancellationToken cancellationToken);

    /// <summary>
    /// Set the file size in <see cref="UploadFileInfo.FileSize"/>
    /// </summary>
    /// <param name="fileId">The file Id</param>
    /// <param name="totalFileSize">The total file size</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>True if set otherwise false</returns>
    Task<bool> SetFileSizeAsync(string fileId, long totalFileSize, CancellationToken cancellationToken);

    /// <summary>
    /// Create file info resource
    /// </summary>
    /// <param name="fileId">The file Id</param>
    /// <param name="fileInfo">The file info</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>True if created otherwise false</returns>
    Task<bool> CreateResourceAsync(string fileId, UploadFileInfo fileInfo, CancellationToken cancellationToken);

    /// <summary>
    /// Set the total uploaded size for the file
    /// </summary>
    /// <param name="fileId">The file Id</param>
    /// <param name="totalBytes">The total bytes</param>
    /// <returns>True if set otherwise false</returns>
    Task<bool> SetTotalUploadedBytesAsync(string fileId, long totalBytes);

    /// <summary>
    /// Set the file path for the file Id
    /// </summary>
    /// <param name="fileId">The file Id</param>
    /// <param name="filePath">The file path</param>
    /// <returns>True if set otherwise false</returns>
    Task<bool> SetFilePathForUploadAsync(string fileId, string filePath);

    /// <summary>
    /// Delete a file upload info
    /// </summary>
    /// <remarks>
    /// Delete meta resource when file has been fully uploaded
    /// </remarks>
    /// <param name="fileId">The file Id</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>True if resource has been deleted otherwise false</returns>
    Task<bool> DeleteUploadFileInfoAsync(string fileId, CancellationToken cancellationToken);
}

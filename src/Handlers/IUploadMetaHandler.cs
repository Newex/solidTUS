using System.Collections.Generic;
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
    /// Create file info resource
    /// </summary>
    /// <param name="fileInfo">The file info</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>True if created otherwise false</returns>
    Task<bool> CreateResourceAsync(UploadFileInfo fileInfo, CancellationToken cancellationToken);

    /// <summary>
    /// Get the upload file info
    /// </summary>
    /// <param name="fileId">The file id</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>An upload file info or null</returns>
    Task<UploadFileInfo?> GetResourceAsync(string fileId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieve all upload file infos
    /// </summary>
    /// <returns>An awaitable collection of upload infos</returns>
    IAsyncEnumerable<UploadFileInfo> GetAllResourcesAsync();

    /// <summary>
    /// Update file info
    /// </summary>
    /// <param name="fileInfo">The updated file info</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Returns true if updated otherwise false</returns>
    Task<bool> UpdateResourceAsync(UploadFileInfo fileInfo, CancellationToken cancellationToken);

    /// <summary>
    /// Delete a file upload info
    /// </summary>
    /// <remarks>
    /// Delete meta resource when file has been fully uploaded
    /// </remarks>
    /// <param name="info">The upload file info</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>True if resource has been deleted otherwise false</returns>
    Task<bool> DeleteUploadFileInfoAsync(UploadFileInfo info, CancellationToken cancellationToken);
}

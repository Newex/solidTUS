using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using SolidTUS.Contexts;
using SolidTUS.Models;

namespace SolidTUS.Handlers;

/// <summary>
/// Upload storage handler
/// </summary>
public interface IUploadStorageHandler
{
    /// <summary>
    /// Partial upload
    /// </summary>
    /// <remarks>
    /// If checksum is used and mismatches, this partial upload must be rolled back
    /// </remarks>
    /// <param name="reader">The pipe reader</param>
    /// <param name="uploadInfo">The upload information</param>
    /// <param name="checksumContext">The checksum context</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The number of bytes appended</returns>
    Task<long> OnPartialUploadAsync(PipeReader reader, UploadFileInfo uploadInfo, ChecksumContext? checksumContext, CancellationToken cancellationToken);

    /// <summary>
    /// Delete uploaded resource including the metadata
    /// </summary>
    /// <param name="uploadFileInfo">The upload file info</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>An awaitable task</returns>
    Task DeleteFileAsync(UploadFileInfo uploadFileInfo, CancellationToken cancellationToken);

    /// <summary>
    /// Get the current upload size
    /// </summary>
    /// <param name="fileId">The file Id</param>
    /// <param name="uploadInfo">The upload information</param>
    /// <returns>The number of bytes uploaded so far or null</returns>
    long? GetUploadSize(string fileId, UploadFileInfo uploadInfo);
}

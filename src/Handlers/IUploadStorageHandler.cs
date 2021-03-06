using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
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
    /// <param name="fileId">The file Id</param>
    /// <param name="reader">The pipe reader</param>
    /// <param name="offset">The byte offset</param>
    /// <param name="expectedSize">The current upload expected size</param>
    /// <param name="append">True if the partial upload can be appended immidiately otherwise false</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The number of bytes appended</returns>
    Task<long> OnPartialUploadAsync(string fileId, PipeReader reader, long offset, long? expectedSize, bool append, CancellationToken cancellationToken);

    /// <summary>
    /// Discard the recently uploaded partial file, due to checksum mismatch and reset the byte offset
    /// </summary>
    /// <param name="fileId">The file Id</param>
    /// <param name="toByteOffset">The original byte offset</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>True if successfully discarded partial upload otherwise false</returns>
    Task<bool> OnDiscardPartialUploadAsync(string fileId, long toByteOffset, CancellationToken cancellationToken);

    /// <summary>
    /// Partial upload checksum is valid therefore the partial upload has succedeed
    /// </summary>
    /// <remarks>
    /// The <see cref="FileUploadStorageHandler"/> appends recently uploaded file to the resulting uploaded file
    /// </remarks>
    /// <param name="fileId">The file Id</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>True if upload success has been handled gracefully otherwise false</returns>
    Task<bool> OnPartialUploadSucceededAsync(string fileId, CancellationToken cancellationToken);

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
    /// <param name="fileInfo">The file info</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>True if created otherwise false</returns>
    Task<bool> CreateResourceAsync(UploadFileInfo fileInfo, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieve the recently uploaded partial file
    /// </summary>
    /// <remarks>
    /// Used to validate partial upload checksum
    /// </remarks>
    /// <param name="fileId">The file Id</param>
    /// <param name="uploadSize">The current chunk upload size</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A stream of the partial upload</returns>
    Task<Stream?> GetPartialUploadedStreamAsync(string fileId, long uploadSize, CancellationToken cancellationToken);
}

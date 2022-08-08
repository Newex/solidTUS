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
    /// <param name="uploadInfo">The upload information</param>
    /// <param name="expectedSize">The current upload expected size</param>
    /// <param name="append">True if the partial upload can be appended immidiately otherwise false</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The number of bytes appended</returns>
    Task<long> OnPartialUploadAsync(string fileId, PipeReader reader, UploadFileInfo uploadInfo, long? expectedSize, bool append, CancellationToken cancellationToken);

    /// <summary>
    /// Discard the recently uploaded partial file, due to checksum mismatch and reset the byte offset
    /// </summary>
    /// <param name="fileId">The file Id</param>
    /// <param name="resetOffset">The original byte offset</param>
    /// <param name="uploadInfo">The upload information</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>True if successfully discarded partial upload otherwise false</returns>
    Task<bool> OnDiscardPartialUploadAsync(string fileId, long resetOffset, UploadFileInfo uploadInfo, CancellationToken cancellationToken);

    /// <summary>
    /// Partial upload checksum is valid therefore the partial upload has succedeed
    /// </summary>
    /// <remarks>
    /// The <see cref="FileUploadStorageHandler"/> appends recently uploaded file to the resulting uploaded file
    /// </remarks>
    /// <param name="fileId">The file Id</param>
    /// <param name="uploadInfo">The upload information</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>True if upload success has been handled gracefully otherwise false</returns>
    Task<bool> OnPartialUploadSucceededAsync(string fileId, UploadFileInfo uploadInfo, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieve the recently uploaded partial file
    /// </summary>
    /// <remarks>
    /// Used to validate partial upload checksum
    /// </remarks>
    /// <param name="fileId">The file Id</param>
    /// <param name="uploadSize">The current chunk upload size</param>
    /// <param name="uploadInfo">The upload information</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A stream of the partial upload</returns>
    Task<Stream?> GetPartialUploadedStreamAsync(string fileId, long uploadSize, UploadFileInfo uploadInfo, CancellationToken cancellationToken);

    /// <summary>
    /// Get the current upload size
    /// </summary>
    /// <param name="fileId">The file Id</param>
    /// <param name="filePath">The optional file path</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The number of bytes uploaded so far or null</returns>
    ValueTask<long?> GetUploadSizeAsync(string fileId, string filePath, CancellationToken cancellationToken);
}

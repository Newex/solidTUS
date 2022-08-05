using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

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
    /// <param name="filePath">The optional file path</param>
    /// <returns>The number of bytes appended</returns>
    Task<long> OnPartialUploadAsync(string fileId, PipeReader reader, long offset, long? expectedSize, bool append, CancellationToken cancellationToken, string? filePath = null);

    /// <summary>
    /// Discard the recently uploaded partial file, due to checksum mismatch and reset the byte offset
    /// </summary>
    /// <param name="fileId">The file Id</param>
    /// <param name="toByteOffset">The original byte offset</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <param name="filePath">The optional file path</param>
    /// <returns>True if successfully discarded partial upload otherwise false</returns>
    Task<bool> OnDiscardPartialUploadAsync(string fileId, long toByteOffset, CancellationToken cancellationToken, string? filePath = null);

    /// <summary>
    /// Partial upload checksum is valid therefore the partial upload has succedeed
    /// </summary>
    /// <remarks>
    /// The <see cref="FileUploadStorageHandler"/> appends recently uploaded file to the resulting uploaded file
    /// </remarks>
    /// <param name="fileId">The file Id</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <param name="filePath">The optional file path</param>
    /// <returns>True if upload success has been handled gracefully otherwise false</returns>
    Task<bool> OnPartialUploadSucceededAsync(string fileId, CancellationToken cancellationToken, string? filePath = null);

    /// <summary>
    /// Retrieve the recently uploaded partial file
    /// </summary>
    /// <remarks>
    /// Used to validate partial upload checksum
    /// </remarks>
    /// <param name="fileId">The file Id</param>
    /// <param name="uploadSize">The current chunk upload size</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <param name="filePath">The optional file path</param>
    /// <returns>A stream of the partial upload</returns>
    Task<Stream?> GetPartialUploadedStreamAsync(string fileId, long uploadSize, CancellationToken cancellationToken, string? filePath = null);

    /// <summary>
    /// Get the current upload size
    /// </summary>
    /// <param name="fileId">The file Id</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <param name="filePath">The optional file path</param>
    /// <returns>The number of bytes uploaded so far or null</returns>
    ValueTask<long?> GetUploadSizeAsync(string fileId, CancellationToken cancellationToken, string? filePath = null);
}

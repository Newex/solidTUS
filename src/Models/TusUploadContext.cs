using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SolidTUS.Handlers;

namespace SolidTUS.Models;

/// <summary>
/// TUS context that is injected into the marked action method
/// </summary>
public class TusUploadContext
{
    private readonly long? expectedUploadSize;
    private readonly IUploadStorageHandler uploadStorageHandler;
    private readonly PipeReader reader;
    private readonly Action<long> onDone;
    private readonly Action<HttpError> onError;
    private readonly CancellationToken cancellationToken;
    private Func<UploadFileInfo, Task>? onUploadFinishedAsync;
    private readonly ChecksumContext? checksumContext;

    /// <summary>
    /// Instantiates a new object of <see cref="TusUploadContext"/>
    /// </summary>
    /// <param name="checksumContext">The checksum context</param>
    /// <param name="expectedUploadSize">The expected current upload size</param>
    /// <param name="uploadStorageHandler">The upload storage handler</param>
    /// <param name="reader">The Pipereader</param>
    /// <param name="onDone">Callback for when done uploading</param>
    /// <param name="onError">Callback for when an error occurs</param>
    /// <param name="uploadFileInfo">The upload file info</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public TusUploadContext(
        ChecksumContext? checksumContext,
        long? expectedUploadSize,
        IUploadStorageHandler uploadStorageHandler,
        PipeReader reader,
        Action<long> onDone,
        Action<HttpError> onError,
        UploadFileInfo uploadFileInfo,
        CancellationToken cancellationToken
    )
    {
        this.expectedUploadSize = expectedUploadSize;
        this.uploadStorageHandler = uploadStorageHandler;
        this.reader = reader;
        this.onDone = onDone;
        this.onError = onError;
        this.cancellationToken = cancellationToken;
        UploadFileInfo = uploadFileInfo;
        this.checksumContext = checksumContext;
    }

    /// <summary>
    /// The requested upload file
    /// </summary>
    public UploadFileInfo UploadFileInfo { get; }

    internal bool UploadHasBeenCalled { get; private set; }

    /// <summary>
    /// Callback for when the file has finished uploading
    /// </summary>
    /// <param name="callback"></param>
    public void OnUploadFinished(Func<UploadFileInfo, Task> callback)
    {
        onUploadFinishedAsync = callback;
    }

    /// <summary>
    /// Start appending data as specified in the <see cref="UploadFileInfo"/>
    /// </summary>
    /// <remarks>
    /// When this method is invoked, it calls the <see cref="IUploadStorageHandler"/> append method
    /// </remarks>
    /// <returns>An awaitable task</returns>
    /// <param name="fileId">The file Id</param>
    /// <exception cref="InvalidOperationException">Thrown when missing the upload file info</exception>
    public async Task StartAppendDataAsync(string fileId)
    {
        UploadHasBeenCalled = true;

        // Can append if we dont need to worry about checksum
        var hasChecksum = checksumContext is not null;
        var savedBytes = await uploadStorageHandler.OnPartialUploadAsync(fileId, reader, UploadFileInfo.ByteOffset, expectedUploadSize, !hasChecksum, cancellationToken);
        var totalSavedBytes = UploadFileInfo.ByteOffset + savedBytes;

        // Determine if the checksum is valid
        var isValidChecksum = true;
        if (hasChecksum)
        {
            var discarded = false;
            try
            {
                using var stream = await uploadStorageHandler.GetPartialUploadedStreamAsync(fileId, savedBytes, cancellationToken);
                if (stream is null)
                {
                    const string Message = "Could not get the uploaded stream to validate checksum";
                    discarded = await DiscardUploadedDataAsync(fileId, UploadFileInfo.ByteOffset, Message, cancellationToken);
                    return;
                }

                var checksum = checksumContext!.Checksum;
                isValidChecksum = await checksumContext!.Validator.ValidateChecksumAsync(stream, checksum);

                if (isValidChecksum)
                {
                    var append = await uploadStorageHandler.OnPartialUploadSucceededAsync(fileId, cancellationToken);
                    if (!append)
                    {
                        const string Message = "Could not append the uploaded file into the original file";
                        discarded = await DiscardUploadedDataAsync(fileId, UploadFileInfo.ByteOffset, Message, cancellationToken);
                        return;
                    }
                }
                else
                {
                    // Hopefully the byte offset has been reset?!
                    const string Message = "Could not reset byte offset after checksum mismatch";
                    discarded = await uploadStorageHandler.OnDiscardPartialUploadAsync(fileId, UploadFileInfo.ByteOffset, cancellationToken);
                    if (!discarded)
                    {
                        onError(HttpError.InternalServerError(Message));
                        return;
                    }

                    var error = HttpError.ChecksumMismatch("The given checksum does not match to the uploaded chunk");
                    onError(error);
                    return;
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (!discarded)
                {
                    _ = await uploadStorageHandler.OnDiscardPartialUploadAsync(fileId, UploadFileInfo.ByteOffset, cancellationToken);
                }
            }
        }

        onDone(totalSavedBytes);

        if (isValidChecksum && UploadFileInfo.FileSize == totalSavedBytes && onUploadFinishedAsync is not null)
        {
            await onUploadFinishedAsync(UploadFileInfo);
        }
    }

    /// <summary>
    /// Deny an upload
    /// </summary>
    /// <param name="status">The response status code</param>
    /// <param name="message">The optional response message</param>
    /// <exception cref="InvalidOperationException">Thrown if upload has already been started. Cannot accept upload and deny at the same time</exception>
    public void TerminateUpload(int status = 400, string? message = null)
    {
        if (UploadHasBeenCalled)
        {
            throw new InvalidOperationException("Cannot upload and terminate request at the same time");
        }

        UploadHasBeenCalled = true;
        var error = new HttpError(status, new HeaderDictionary(), message);
        onError(error);
    }

    private async Task<bool> DiscardUploadedDataAsync(string fileId, long offset, string? message, CancellationToken cancellationToken)
    {
        var discarded = await uploadStorageHandler.OnDiscardPartialUploadAsync(fileId, offset, cancellationToken);
        if (!discarded)
        {
            // Completely inconsistent state! What to do? Could not delete partial upload
            onError(HttpError.InternalServerError());
            return false;
        }

        var error = HttpError.InternalServerError(message);
        onError(error);
        return true;
    }
}

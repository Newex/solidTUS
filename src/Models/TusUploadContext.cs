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
    private readonly UploadFileInfo uploadFileInfo;
    private readonly long? expectedUploadSize;
    private readonly IUploadMetaHandler uploadMetaHandler;
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
    /// <param name="uploadMetaHandler">The upload meta handler</param>
    /// <param name="uploadStorageHandler">The upload storage handler</param>
    /// <param name="reader">The Pipereader</param>
    /// <param name="onDone">Callback for when done uploading</param>
    /// <param name="onError">Callback for when an error occurs</param>
    /// <param name="uploadFileInfo">The upload file info</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public TusUploadContext(
        ChecksumContext? checksumContext,
        long? expectedUploadSize,
        IUploadMetaHandler uploadMetaHandler,
        IUploadStorageHandler uploadStorageHandler,
        PipeReader reader,
        Action<long> onDone,
        Action<HttpError> onError,
        UploadFileInfo uploadFileInfo,
        CancellationToken cancellationToken
    )
    {
        this.expectedUploadSize = expectedUploadSize;
        this.uploadMetaHandler = uploadMetaHandler;
        this.uploadStorageHandler = uploadStorageHandler;
        this.reader = reader;
        this.onDone = onDone;
        this.onError = onError;
        this.cancellationToken = cancellationToken;
        this.uploadFileInfo = uploadFileInfo;
        this.checksumContext = checksumContext;
    }

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
    /// Start appending data
    /// </summary>
    /// <remarks>
    /// When this method is invoked, it calls the <see cref="IUploadStorageHandler"/> append method.
    /// When upload is finished and the <see cref="Models.UploadFileInfo"/> has been deleted then subsequent uploads of the same file will start from the beginning.
    /// If the meta upload file info exists, then the client will be notified that the file already exists and no additional uploads will be done.
    /// </remarks>
    /// <param name="fileId">The file Id</param>
    /// <param name="filePath">The optional file path</param>
    /// <param name="deleteInfoOnDone">True if the metadata upload info file should be deleted when upload has been finished otherwise false</param>
    /// <returns>An awaitable task</returns>
    /// <exception cref="InvalidOperationException">Thrown when missing the upload file info</exception>
    public async Task StartAppendDataAsync(string fileId, string? filePath = null, bool deleteInfoOnDone = false)
    {
        // Only call start append once
        if (UploadHasBeenCalled)
        {
            return;
        }

        UploadHasBeenCalled = true;

        if (filePath is not null && uploadFileInfo.FilePath is null)
        {
            await uploadMetaHandler.SetFilePathForUploadAsync(fileId, filePath);
            uploadFileInfo.FilePath = filePath;
        }
        else if (uploadFileInfo.FilePath is not null)
        {
            // "Load" file path from the resource if there is a pre-existing path
            filePath = uploadFileInfo.FilePath;
        }

        // Can append if we dont need to worry about checksum
        var hasChecksum = checksumContext is not null;
        var savedBytes = await uploadStorageHandler.OnPartialUploadAsync(fileId, reader, uploadFileInfo.ByteOffset, expectedUploadSize, !hasChecksum, cancellationToken, filePath);
        var totalSavedBytes = uploadFileInfo.ByteOffset + savedBytes;

        // Determine if the checksum is valid
        var isValidChecksum = true;
        if (hasChecksum)
        {
            var discarded = false;
            try
            {
                using var stream = await uploadStorageHandler.GetPartialUploadedStreamAsync(fileId, savedBytes, cancellationToken, filePath);
                if (stream is null)
                {
                    const string Message = "Could not get the uploaded stream to validate checksum";
                    discarded = await DiscardUploadedDataAsync(fileId, uploadFileInfo.ByteOffset, Message, filePath, cancellationToken);
                    return;
                }

                var checksum = checksumContext!.Checksum;
                isValidChecksum = await checksumContext!.Validator.ValidateChecksumAsync(stream, checksum);

                if (isValidChecksum)
                {
                    var append = await uploadStorageHandler.OnPartialUploadSucceededAsync(fileId, cancellationToken, filePath);
                    if (!append)
                    {
                        const string Message = "Could not append the uploaded file into the original file";
                        discarded = await DiscardUploadedDataAsync(fileId, uploadFileInfo.ByteOffset, Message, filePath, cancellationToken);
                        return;
                    }
                }
                else
                {
                    // Hopefully the byte offset has been reset?!
                    const string Message = "Could not reset byte offset after checksum mismatch";
                    discarded = await uploadStorageHandler.OnDiscardPartialUploadAsync(fileId, uploadFileInfo.ByteOffset, cancellationToken, filePath);
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
                    _ = await uploadStorageHandler.OnDiscardPartialUploadAsync(fileId, uploadFileInfo.ByteOffset, cancellationToken, filePath);
                }
            }
        }

        onDone(totalSavedBytes);

        var isFinished = isValidChecksum && uploadFileInfo.FileSize == totalSavedBytes;
        if (isFinished && onUploadFinishedAsync is not null)
        {
            await onUploadFinishedAsync(uploadFileInfo);
        }

        if (isFinished && deleteInfoOnDone)
        {
            await uploadMetaHandler.DeleteUploadFileInfoAsync(fileId, cancellationToken);
        }
    }

    /// <summary>
    /// Deny an upload
    /// </summary>
    /// <param name="fileId">The file Id</param>
    /// <param name="status">The response status code</param>
    /// <param name="message">The optional response message</param>
    /// <exception cref="InvalidOperationException">Thrown if upload has already been started. Cannot accept upload and deny at the same time</exception>
    public async void TerminateUpload(string fileId, int status = 400, string? message = null)
    {
        if (UploadHasBeenCalled)
        {
            throw new InvalidOperationException("Cannot upload and terminate request at the same time");
        }

        // Deny and remove metadata
        await uploadMetaHandler.DeleteUploadFileInfoAsync(fileId, cancellationToken);

        UploadHasBeenCalled = true;
        var error = new HttpError(status, new HeaderDictionary(), message);
        onError(error);
    }

    private async Task<bool> DiscardUploadedDataAsync(string fileId, long offset, string? message, string? filePath, CancellationToken cancellationToken)
    {
        var discarded = await uploadStorageHandler.OnDiscardPartialUploadAsync(fileId, offset, cancellationToken, filePath);
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

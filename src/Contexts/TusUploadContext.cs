using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SolidTUS.Handlers;
using SolidTUS.Models;

namespace SolidTUS.Contexts;

/// <summary>
/// TUS context that is injected into the marked action method
/// </summary>
public class TusUploadContext
{
    private readonly IUploadMetaHandler uploadMetaHandler;
    private readonly IUploadStorageHandler uploadStorageHandler;
    private readonly PipeReader reader;
    private readonly Action<UploadFileInfo> onDone;
    private readonly Action<HttpError> onError;
    private readonly CancellationToken cancellationToken;
    private Func<UploadFileInfo, Task>? onUploadFinishedAsync;
    private readonly ChecksumContext? checksumContext;

    /// <summary>
    /// Instantiates a new object of <see cref="TusUploadContext"/>
    /// </summary>
    /// <param name="checksumContext">The checksum context</param>
    /// <param name="uploadMetaHandler">The upload meta handler</param>
    /// <param name="uploadStorageHandler">The upload storage handler</param>
    /// <param name="reader">The Pipereader</param>
    /// <param name="onDone">Callback for when done uploading</param>
    /// <param name="onError">Callback for when an error occurs</param>
    /// <param name="uploadFileInfo">The upload file info</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public TusUploadContext(
        ChecksumContext? checksumContext,
        IUploadMetaHandler uploadMetaHandler,
        IUploadStorageHandler uploadStorageHandler,
        PipeReader reader,
        Action<UploadFileInfo> onDone,
        Action<HttpError> onError,
        UploadFileInfo uploadFileInfo,
        CancellationToken cancellationToken
    )
    {
        this.uploadMetaHandler = uploadMetaHandler;
        this.uploadStorageHandler = uploadStorageHandler;
        this.reader = reader;
        this.onDone = onDone;
        this.onError = onError;
        this.cancellationToken = cancellationToken;
        UploadFileInfo = uploadFileInfo;
        this.checksumContext = checksumContext;
    }

    internal bool UploadHasBeenCalled { get; private set; }

    /// <summary>
    /// Get the upload file info
    /// </summary>
    public UploadFileInfo UploadFileInfo { get; init; }

    /// <summary>
    /// Callback for when the file has finished uploading
    /// </summary>
    /// <remarks>
    /// Must be called prior to starting the upload.
    /// </remarks>
    /// <param name="callback">The callback function</param>
    public void OnUploadFinished(Func<UploadFileInfo, Task> callback)
    {
        onUploadFinishedAsync = callback;
    }

    /// <summary>
    /// Set the individual upload expiration strategy.
    /// </summary>
    /// <remarks>
    /// Must be called prior to starting the upload.
    /// </remarks>
    /// <param name="expiration">The expiration strategy</param>
    /// <param name="interval">The optional time span interval</param>
    public void SetExpirationStrategy(ExpirationStrategy expiration, TimeSpan? interval = null)
    {
        UploadFileInfo.ExpirationStrategy = expiration;
        UploadFileInfo.Interval = interval;
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
    /// <param name="deleteInfoOnDone">True if the metadata upload info file should be deleted when upload has been finished otherwise false</param>
    /// <returns>An awaitable task</returns>
    /// <exception cref="InvalidOperationException">Thrown when missing the upload file info</exception>
    public async Task StartAppendDataAsync(string fileId, bool deleteInfoOnDone = false)
    {
        // Only call start append once
        if (UploadHasBeenCalled)
        {
            return;
        }

        UploadHasBeenCalled = true;

        // Can append if we dont need to worry about checksum
        var savedBytes = await uploadStorageHandler.OnPartialUploadAsync(fileId, reader, UploadFileInfo, checksumContext, cancellationToken);
        onDone(UploadFileInfo);

        if (UploadFileInfo.Done && onUploadFinishedAsync is not null)
        {
            await onUploadFinishedAsync(UploadFileInfo);
        }

        if (UploadFileInfo.Done && deleteInfoOnDone)
        {
            await uploadMetaHandler.DeleteUploadFileInfoAsync(fileId, cancellationToken);
            return;
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
}

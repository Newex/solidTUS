using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SolidTUS.Handlers;
using SolidTUS.Models;
using SolidTUS.Options;

namespace SolidTUS.Contexts;

/// <summary>
/// The TUS creation context
/// </summary>
public class TusCreationContext
{
    private readonly string defaultFileDirectory;
    private readonly bool withUpload;
    private readonly PipeReader reader;
    private readonly IUploadStorageHandler uploadStorageHandler;
    private readonly IUploadMetaHandler uploadMetaHandler;
    private readonly CancellationToken cancellationToken;
    private readonly Action<string> onCreated;
    private readonly Action<long> onUpload;

    private Func<UploadFileInfo, Task>? onResourceCreatedAsync;
    private Func<Task>? onUploadFinishedAsync;

    /// <summary>
    /// Instantiate a new object of <see cref="TusCreationContext"/>
    /// </summary>
    /// <param name="options">The options</param>
    /// <param name="withUpload">True if request includes upload otherwise false</param>
    /// <param name="uploadFileInfo">The upload file info</param>
    /// <param name="onCreated">Callback when resource has been created</param>
    /// <param name="onUpload">Callback when resource has been uploaded</param>
    /// <param name="reader">The upload reader</param>
    /// <param name="uploadStorageHandler">The upload storage handler</param>
    /// <param name="uploadMetaHandler">The upload meta handler</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public TusCreationContext(
        IOptions<FileStorageOptions> options,
        bool withUpload,
        UploadFileInfo uploadFileInfo,
        Action<string> onCreated,
        Action<long> onUpload,
        PipeReader reader,
        IUploadStorageHandler uploadStorageHandler,
        IUploadMetaHandler uploadMetaHandler,
        CancellationToken cancellationToken
)
    {
        defaultFileDirectory = options.Value.DirectoryPath;
        this.withUpload = withUpload;
        UploadFileInfo = uploadFileInfo;
        this.onCreated = onCreated;
        this.onUpload = onUpload;
        this.reader = reader;
        this.uploadStorageHandler = uploadStorageHandler;
        this.uploadMetaHandler = uploadMetaHandler;
        this.cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Get the upload file info
    /// </summary>
    public UploadFileInfo UploadFileInfo { get; init; }

    /// <summary>
    /// Function called when resource has been created
    /// </summary>
    /// <param name="callback">The callback function</param>
    public void OnResourceCreated(Func<UploadFileInfo, Task> callback)
    {
        onResourceCreatedAsync = callback;
    }

    /// <summary>
    /// Function called when an optional creation contains upload data that has finished uploading data
    /// </summary>
    /// <remarks>
    /// Is not called if the upload is only a partial upload
    /// </remarks>
    /// <param name="callback"></param>
    public void OnUploadFinished(Func<Task> callback)
    {
        onUploadFinishedAsync = callback;
    }

    /// <summary>
    /// Set the individual upload expiration strategy
    /// </summary>
    /// <remarks>
    /// Must be called prior to starting the resource creation.
    /// </remarks>
    /// <param name="expiration">The expiration strategy</param>
    /// <param name="interval">The optional time span interval</param>
    public void SetExpirationStrategy(ExpirationStrategy expiration, TimeSpan? interval = null)
    {
        UploadFileInfo.ExpirationStrategy = expiration;
        UploadFileInfo.Interval = interval;
    }

    /// <summary>
    /// Start resource creation
    /// </summary>
    /// <remarks>
    /// The metadata upload file info will only be deleted if the request contains the whole upload. Otherwise the client will be directed to the upload location.
    /// It is recommended to create a unique and different filename to avoid any malicious uploads overwriting other files.
    /// The metadata.json file that is created by default by <see cref="FileUploadMetaHandler"/> creates a filename using the file id.
    /// Be careful not to overwrite other uploads that are in progress by naming them the same.
    /// </remarks>
    /// <param name="fileId">The file Id</param>
    /// <param name="uploadLocationUrl">The upload location URL</param>
    /// <param name="directoryPath">The optional file directory path</param>
    /// <param name="filename">The filename on disk. Defaults to <paramref name="fileId"/> value</param>
    /// <param name="deleteInfoOnDone">True if the metadata upload info file should be deleted when upload has been finished otherwise false</param>
    /// <returns>An awaitable task</returns>
    public async Task StartCreationAsync(string fileId, string uploadLocationUrl, string? directoryPath = null, string? filename = null, bool deleteInfoOnDone = false)
    {
        UploadFileInfo.OnDiskFilename = filename ?? fileId;
        UploadFileInfo.FileDirectoryPath = directoryPath ?? defaultFileDirectory;
        UploadFileInfo.FileId = fileId;
        var created = await uploadMetaHandler.CreateResourceAsync(UploadFileInfo, cancellationToken);
        if (created)
        {
            // Server side callback
            onCreated(uploadLocationUrl);

            if (onResourceCreatedAsync is not null)
            {
                // Client side callback
                await onResourceCreatedAsync(UploadFileInfo);
            }
        }

        if (withUpload)
        {
            // Can append if we dont need to worry about checksum
            var written = await uploadStorageHandler.OnPartialUploadAsync(fileId, reader, UploadFileInfo, null, cancellationToken);

            // First server callback -->
            onUpload(written);

            // Finished upload -->
            var isFinished = written == UploadFileInfo.FileSize;
            if (isFinished && onUploadFinishedAsync is not null)
            {
                // Client callback -->
                await onUploadFinishedAsync();
            }

            if (isFinished && deleteInfoOnDone)
            {
                await uploadMetaHandler.DeleteUploadFileInfoAsync(fileId, cancellationToken);
            }
        }
    }
}

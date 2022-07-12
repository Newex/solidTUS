using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using SolidTUS.Handlers;

namespace SolidTUS.Models;

/// <summary>
/// The TUS creation context
/// </summary>
public class TusCreationContext
{
    private readonly bool withUpload;
    private readonly PipeReader reader;
    private readonly IUploadStorageHandler uploadStorageHandler;
    private readonly CancellationToken cancellationToken;
    private readonly string? rawMetadata;
    private readonly long? fileSize;
    private readonly Action<string> onCreated;
    private readonly Action<long> onUpload;

    private Func<UploadFileInfo, Task>? onResourceCreatedAsync;
    private Func<Task>? onUploadFinishedAsync;

    /// <summary>
    /// Instantiate a new object of <see cref="TusCreationContext"/>
    /// </summary>
    /// <param name="withUpload">True if request includes upload otherwise false</param>
    /// <param name="rawMetadata">The raw metadata</param>
    /// <param name="metadata">The parsed metadata</param>
    /// <param name="fileSize">The file size</param>
    /// <param name="onCreated">Callback when resource has been created</param>
    /// <param name="onUpload">Callback when resource has been uploaded</param>
    /// <param name="reader">The upload reader</param>
    /// <param name="uploadStorageHandler">The upload storage handler</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public TusCreationContext(
        bool withUpload,
        string? rawMetadata,
        Dictionary<string, string> metadata,
        long? fileSize,
        Action<string> onCreated,
        Action<long> onUpload,
        PipeReader reader,
        IUploadStorageHandler uploadStorageHandler,
        CancellationToken cancellationToken
)
    {
        this.withUpload = withUpload;
        this.rawMetadata = rawMetadata;
        Metadata = new(metadata);
        this.fileSize = fileSize;
        this.onCreated = onCreated;
        this.onUpload = onUpload;
        this.reader = reader;
        this.uploadStorageHandler = uploadStorageHandler;
        this.cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Get or set the filename
    /// </summary>
    public string ActualFileName { get; set; } = string.Empty;

    /// <summary>
    /// Get or set the MIME type
    /// </summary>
    public string MimeType { get; set; } = MediaTypeNames.Application.Octet;

    /// <summary>
    /// Get the upload metadata
    /// </summary>
    public ReadOnlyDictionary<string, string> Metadata { get; init; }

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
    /// Start resource creation
    /// </summary>
    /// <param name="fileId">The file Id</param>
    /// <param name="uploadLocationUrl">The upload location URL</param>
    /// <returns>An awaitable task</returns>
    public async Task StartCreationAsync(string fileId, string uploadLocationUrl)
    {
        var uploadFileInfo = new UploadFileInfo
        {
            ID = fileId,
            ActualFilename = ActualFileName,
            MimeType = MimeType,
            Metadata = Metadata.ToDictionary(item => item.Key, item => item.Value),
            RawMetadata = rawMetadata,
            FileSize = fileSize,
        };
        var created = await uploadStorageHandler.CreateResourceAsync(uploadFileInfo, cancellationToken);

        if (created)
        {
            // Server side callback
            onCreated(uploadLocationUrl);

            if (onResourceCreatedAsync is not null)
            {
                // Client side callback
                await onResourceCreatedAsync(uploadFileInfo);
            }
        }

        if (withUpload)
        {
            // Can append if we dont need to worry about checksum
            var written = await uploadStorageHandler.OnPartialUploadAsync(uploadFileInfo.ID, reader, 0L, uploadFileInfo.FileSize, true, cancellationToken);

            // First server callback -->
            onUpload(written);

            // Finished upload -->
            if (onUploadFinishedAsync is not null && written == uploadFileInfo.FileSize)
            {
                // Client callback -->
                await onUploadFinishedAsync();
            }
        }
    }
}

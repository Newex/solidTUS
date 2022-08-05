using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SolidTUS.Options;

namespace SolidTUS.Handlers;

/// <summary>
/// Simple naive file storage handler
/// </summary>
public class FileUploadStorageHandler : IUploadStorageHandler
{
    private readonly string directoryPath;
    private readonly IUploadMetaHandler uploadMetaHandler;

    /// <summary>
    /// Instantiate a new object of <see cref="FileUploadStorageHandler"/>
    /// </summary>
    /// <param name="uploadMetaHandler">The upload meta handler</param>
    /// <param name="options">The file storage options</param>
    public FileUploadStorageHandler(
        IUploadMetaHandler uploadMetaHandler,
        IOptions<FileStorageOptions> options
    )
    {
        directoryPath = options.Value.DirectoryPath;
        this.uploadMetaHandler = uploadMetaHandler;
    }

    /// <inheritdoc />
    public async Task<long> OnPartialUploadAsync(string fileId, PipeReader reader, long offset, long? expectedSize, bool append, CancellationToken cancellationToken)
    {
        // Get upload file info metadata
        var fileInfo = await uploadMetaHandler.GetUploadFileInfoAsync(fileId, cancellationToken);
        if (fileInfo is null)
        {
            throw new InvalidOperationException("Missing resource data");
        }

        var hasContentLength = expectedSize.HasValue;
        var written = 0L;
        var fileExists = UploadFileExists(fileId);
        var firstCreation = offset == 0 && !fileExists;

        if (!firstCreation)
        {
            var currentSize = fileInfo.ByteOffset;
            var sameOffset = currentSize == offset;
            if (!sameOffset)
            {
                throw new ArgumentException("Mismatching bytes between expected offset and actual offset", nameof(offset));
            }
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // System.IO.IOException client reset request stream
                var result = await reader.ReadAsync(cancellationToken);
                var buffer = result.Buffer;
                var filename = append ? FullFilenamePath(fileId) : FullChunkFilenamePath(fileId);
                var writeMode = append ? FileMode.Append : FileMode.Create;
                using var fs = new FileStream(filename, writeMode);
                var end = (int)buffer.Length;
                await fs.WriteAsync(buffer.ToArray().AsMemory(0, end), cancellationToken);

                written += end;
                reader.AdvanceTo(buffer.GetPosition(end));
                if (hasContentLength && written > expectedSize)
                {
                    throw new ArgumentException("Wrote more data than expected", nameof(expectedSize));
                }
                if (result.IsCompleted)
                {
                    break;
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            var total = fileInfo.ByteOffset + written;
            await uploadMetaHandler.SetTotalUploadedBytesAsync(fileId, total);
        }

        return written;
    }

    /// <inheritdoc />
    public Task<Stream?> GetPartialUploadedStreamAsync(string fileId, long uploadSize, CancellationToken cancellationToken)
    {
        var chunkPath = FullChunkFilenamePath(fileId);
        var hasChunk = File.Exists(chunkPath);
        var filename = FullFilenamePath(fileId);
        var hasFile = File.Exists(filename);

        if (!hasChunk && !hasFile)
        {
            return Task.FromResult<Stream?>(null);
        }

        var path = hasChunk ? chunkPath : filename;
        var fileSize = new FileInfo(path).Length;
        if (fileSize != uploadSize)
        {
            // Delete the stale chunk
            if (hasChunk)
            {
                File.Delete(path);
            }

            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(File.OpenRead(path));
    }

    /// <inheritdoc />
    public async Task<bool> OnDiscardPartialUploadAsync(string fileId, long toByteOffset, CancellationToken cancellationToken)
    {
        var chunkPath = FullChunkFilenamePath(fileId);
        var hasChunk = File.Exists(chunkPath);
        var filename = FullFilenamePath(fileId);
        var hasFile = File.Exists(filename);

        if (!hasChunk && !hasFile)
        {
            return false;
        }

        var path = hasChunk ? chunkPath : filename;
        File.Delete(path);
        await uploadMetaHandler.SetTotalUploadedBytesAsync(fileId, toByteOffset);
        return !File.Exists(path);
    }

    /// <inheritdoc />
    public Task<bool> OnPartialUploadSucceededAsync(string fileId, CancellationToken cancellationToken)
    {
        var chunkPath = FullChunkFilenamePath(fileId);
        var hasChunk = File.Exists(chunkPath);
        var filename = FullFilenamePath(fileId);
        var hasFile = File.Exists(filename);

        if (!hasChunk && !hasFile)
        {
            return Task.FromResult(false);
        }

        if (hasChunk)
        {
            var file = new FileStream(filename, FileMode.Append);
            var chunk = new FileStream(chunkPath, FileMode.Open, FileAccess.Read);

            chunk.CopyTo(file);
            File.Delete(chunkPath);
        }

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public ValueTask<long?> GetUploadSizeAsync(string fileId, CancellationToken cancellationToken)
    {
        var filename = FullFilenamePath(fileId);
        var exists = File.Exists(filename);
        if (!exists)
        {
            return new ValueTask<long?>();
        }

        var size = new FileInfo(filename).Length;
        return new ValueTask<long?>(size);
    }

    private bool UploadFileExists(string fileId) => File.Exists(FullFilenamePath(fileId));
    private string FullFilenamePath(string fileId) => Path.Combine(directoryPath, fileId);
    private string FullChunkFilenamePath(string fileId) => Path.Combine(directoryPath, $"{fileId}.chunk");
}

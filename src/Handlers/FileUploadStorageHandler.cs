using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SolidTUS.Models;
using SolidTUS.Options;

namespace SolidTUS.Handlers;

/// <summary>
/// Simple naive file storage handler
/// </summary>
public class FileUploadStorageHandler : IUploadStorageHandler
{
    private readonly string directoryPath;

    /// <summary>
    /// Instantiate a new object of <see cref="FileUploadStorageHandler"/>
    /// </summary>
    /// <param name="options">The file storage options</param>
    public FileUploadStorageHandler(
        IOptions<FileStorageOptions> options
    )
    {
        directoryPath = options.Value.DirectoryPath;
    }

    /// <inheritdoc />
    public async Task<long> OnPartialUploadAsync(string fileId, PipeReader reader, long offset, long? expectedSize, bool append, CancellationToken cancellationToken)
    {
        // Get upload file info metadata
        var fileInfo = ReadUploadFileInfo(fileId);
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
            // Always update file info metadata; If client disconnects
            var updatedFileInfo = fileInfo with
            {
                ByteOffset = fileInfo.ByteOffset + written
            };
            WriteUploadFileInfo(updatedFileInfo);
        }

        return written;
    }

    /// <inheritdoc />
    public Task<bool> CreateResourceAsync(UploadFileInfo fileInfo, CancellationToken cancellationToken)
    {
        try
        {
            WriteUploadFileInfo(fileInfo);
        }
        catch (Exception)
        {
            // Oops
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<UploadFileInfo?> GetUploadFileInfoAsync(string fileId, CancellationToken cancellationToken)
    {
        var fileInfo = ReadUploadFileInfo(fileId);
        var filename = $"{fileId}";
        var filePath = Path.Combine(directoryPath, filename);
        var fileExists = File.Exists(filePath);

        if (fileInfo is null)
        {
            return Task.FromResult<UploadFileInfo?>(null);
        }

        if (fileExists)
        {
            var byteOffset = new FileInfo(filePath).Length;

            // REASON: we cannot count on the file metadata being up to date if any error occurred
            // what is the source of truth is the saved file size
            // ;If server crashed -->
            if (byteOffset != fileInfo.ByteOffset)
            {
                // Update metadata
                var update = fileInfo with
                {
                    ByteOffset = byteOffset
                };
                WriteUploadFileInfo(update);
                return Task.FromResult<UploadFileInfo?>(update);
            }
        }
        else
        {
            // No file exists therefore offset should be from 0
            var isZero = fileInfo.ByteOffset == 0;
            if (!isZero)
            {
                var update = fileInfo with
                {
                    ByteOffset = 0
                };
                WriteUploadFileInfo(update);
                return Task.FromResult<UploadFileInfo?>(update);
            }
        }

        return Task.FromResult<UploadFileInfo?>(fileInfo);
    }

    /// <inheritdoc />
    public Task<bool> SetFileSizeAsync(string fileId, long totalFileSize, CancellationToken cancellationToken)
    {
        var fileInfo = ReadUploadFileInfo(fileId);
        if (fileInfo is not null)
        {
            var updates = fileInfo with
            {
                FileSize = totalFileSize
            };

            WriteUploadFileInfo(updates);

            return Task.FromResult(true);
        }

        return Task.FromResult(false);
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
    public Task<bool> OnDiscardPartialUploadAsync(string fileId, long toByteOffset, CancellationToken cancellationToken)
    {
        var chunkPath = FullChunkFilenamePath(fileId);
        var hasChunk = File.Exists(chunkPath);
        var filename = FullFilenamePath(fileId);
        var hasFile = File.Exists(filename);

        if (!hasChunk && !hasFile)
        {
            return Task.FromResult(false);
        }

        var path = hasChunk ? chunkPath : filename;
        File.Delete(path);
        var fileInfo = ReadUploadFileInfo(fileId);
        if (fileInfo is null)
        {
            return Task.FromResult(false);
        }

        WriteUploadFileInfo(fileInfo with
        {
            ByteOffset = toByteOffset
        });

        var deleted = !File.Exists(path);
        return Task.FromResult(deleted);
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

    private UploadFileInfo? ReadUploadFileInfo(string fileId)
    {
        var filename = MetadataFullFilenamePath(fileId);
        var fileInfoTxt = File.ReadAllText(filename);
        var fileInfo = JsonSerializer.Deserialize<UploadFileInfo>(fileInfoTxt);
        return fileInfo!;
    }

    private void WriteUploadFileInfo(UploadFileInfo fileInfo)
    {
        var filename = MetadataFullFilenamePath(fileInfo.ID);
        var content = JsonSerializer.Serialize(fileInfo);
        File.WriteAllText(filename, content);
    }

    private bool UploadFileExists(string fileId) => File.Exists(FullFilenamePath(fileId));
    private string MetadataFullFilenamePath(string fileId) => Path.Combine(directoryPath, $"{fileId}.metadata.json");
    private string FullFilenamePath(string fileId) => Path.Combine(directoryPath, fileId);
    private string FullChunkFilenamePath(string fileId) => Path.Combine(directoryPath, $"{fileId}.chunk");
}

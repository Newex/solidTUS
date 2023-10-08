using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using SolidTUS.Contexts;
using SolidTUS.Models;
using SolidTUS.Options;

namespace SolidTUS.Handlers;

/// <summary>
/// Simple naive file storage handler
/// </summary>
public class FileUploadStorageHandler : IUploadStorageHandler
{
    private readonly ISystemClock clock;
    private readonly string directory;

    /// <summary>
    /// Instantiate a new <see cref="FileUploadStorageHandler"/>
    /// </summary>
    /// <param name="clock">The system clock provider</param>
    /// <param name="options"></param>
    public FileUploadStorageHandler(
        ISystemClock clock,
        IOptions<FileStorageOptions> options
    )
    {
        this.clock = clock;
        directory = options.Value.DirectoryPath;
    }

    /// <inheritdoc />
    public async Task<long> OnPartialUploadAsync(PipeReader reader, UploadFileInfo uploadInfo, ChecksumContext? checksumContext, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var written = 0L;
        var withChecksum = checksumContext is not null;
        var validChecksum = true && !withChecksum;

        try
        {
            var filename = FullFilenamePath(uploadInfo.OnDiskFilename, directory);
            uploadInfo.OnDiskDirectoryPath = Path.GetDirectoryName(Path.GetFullPath(filename));

            using var fs = new FileStream(filename, FileMode.Append, FileAccess.Write);

            BufferedStream? bs = null;
            Pipe? checksumPipe = null;
            PipeReader? checksumReader = null;
            PipeWriter? checksumWriter = null;
            Task<bool>? checksumValid = null;

            if (withChecksum)
            {
                ArgumentNullException.ThrowIfNull(checksumContext);

                bs = new BufferedStream(fs);
                checksumPipe = new Pipe();
                checksumReader = checksumPipe.Reader;
                checksumWriter = checksumPipe.Writer;
                checksumValid = checksumContext.Validator.ValidateChecksumAsync(checksumReader, checksumContext.Checksum);
            }

            while (true)
            {
                // System.IO.IOException client reset request stream
                var result = await reader.ReadAsync(cancellationToken);
                var buffer = result.Buffer;
                var end = (int)buffer.Length;
                await fs.WriteAsync(buffer.ToArray().AsMemory(0, end), cancellationToken);

                if (withChecksum)
                {
                    ArgumentNullException.ThrowIfNull(bs);
                    ArgumentNullException.ThrowIfNull(checksumWriter);

                    bs.CopyTo(checksumWriter.AsStream());
                }

                written += end;
                reader.AdvanceTo(buffer.GetPosition(end));
                if (result.IsCompleted)
                {
                    break;
                }
            }

            if (withChecksum)
            {
                ArgumentNullException.ThrowIfNull(checksumValid);

                validChecksum = await checksumValid;
                if (!validChecksum)
                {
                    // Truncate file
                    fs.SetLength(uploadInfo.ByteOffset);
                }
            }
        }
        catch (IOException) { }
        finally
        {
            if (validChecksum)
            {
                uploadInfo.AddBytes(written);
                uploadInfo.LastUpdatedDate = clock.UtcNow;
            }
        }

        return written;
    }

    /// <inheritdoc />
    public long? GetUploadSize(string fileId, UploadFileInfo uploadInfo)
    {
        var filename = FullFilenamePath(uploadInfo.OnDiskFilename, directory);
        var exists = File.Exists(filename);
        if (!exists)
        {
            return null;
        }

        var size = new FileInfo(filename).Length;
        return size;
    }

    /// <inheritdoc />
    public Task DeleteFileAsync(UploadFileInfo uploadFileInfo, CancellationToken cancellationToken)
    {
        var file = Path.Combine(directory, uploadFileInfo.OnDiskFilename);
        File.Delete(file);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<UploadFileInfo> MergePartialFilesAsync(UploadFileInfo final, IList<UploadFileInfo> files, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var filepath = FullFilenamePath(final.OnDiskFilename, directory);
        if (File.Exists(filepath))
        {
            throw new InvalidOperationException("File exists. Cannot merge partial files on top by overwrite existing file.");
        }

        try
        {
            using var destination = new FileStream(filepath, FileMode.Append);

            foreach (var file in files)
            {
                var otherPath = FullFilenamePath(file.OnDiskFilename, directory);
                using var readFs = File.OpenRead(otherPath);
                await readFs.CopyToAsync(destination, cancellationToken);
            }

            final.CreatedDate = clock.UtcNow;
            final.LastUpdatedDate = null;
            final.AddBytes(destination.Position);
        }
        catch(Exception)
        {
            File.Delete(filepath);
            throw;
        }

        return final;
    }

    private static string FullFilenamePath(string filename, string filePath)
    {
        return Path.Combine(filePath, filename);
    }
}

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using SolidTUS.Contexts;
using SolidTUS.Models;

namespace SolidTUS.Handlers;

/// <summary>
/// Simple naive file storage handler
/// </summary>
public class FileUploadStorageHandler : IUploadStorageHandler
{
    private readonly IUploadMetaHandler uploadMetaHandler;

    /// <summary>
    /// Instantiate a new <see cref="FileUploadStorageHandler"/>
    /// </summary>
    /// <param name="uploadMetaHandler">The metadata file handler</param>
    public FileUploadStorageHandler(IUploadMetaHandler uploadMetaHandler)
    {
        this.uploadMetaHandler = uploadMetaHandler;
    }

    /// <inheritdoc />
    public async Task<long> OnPartialUploadAsync(string fileId, PipeReader reader, UploadFileInfo uploadInfo, ChecksumContext? checksumContext, CancellationToken cancellationToken)
    {
        var written = 0L;
        var withChecksum = checksumContext is not null;
        var validChecksum = true && !withChecksum;

        try
        {
            var filename = FullFilenamePath(uploadInfo.OnDiskFilename, uploadInfo.FileDirectoryPath);

            using var fs = new FileStream(filename, FileMode.Append);

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
        catch (IOException)
        {

        }
        finally
        {
            if (validChecksum)
            {
                uploadInfo.AddBytes(written);
                await uploadMetaHandler.UpdateResourceAsync(uploadInfo, cancellationToken);
            }
        }

        return written;
    }

    /// <inheritdoc />
    public long? GetUploadSize(string fileId, UploadFileInfo uploadInfo)
    {
        var filename = FullFilenamePath(uploadInfo.OnDiskFilename, uploadInfo.FileDirectoryPath);
        var exists = File.Exists(filename);
        if (!exists)
        {
            return null;
        }

        var size = new FileInfo(filename).Length;
        return size;
    }

    private static string FullFilenamePath(string filename, string filePath)
    {
        return Path.Combine(filePath, filename);
    }

    private static string FullChunkFilenamePath(string filename, string filePath)
    {
        return Path.Combine(filePath, $"{filename}.chunk");
    }
}

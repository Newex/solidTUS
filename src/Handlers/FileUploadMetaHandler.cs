using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SolidTUS.Models;
using SolidTUS.Options;

namespace SolidTUS.Handlers;

/// <summary>
/// File upload meta handler
/// </summary>
public class FileUploadMetaHandler : IUploadMetaHandler
{
    private static readonly ReaderWriterLockSlim Rwl = new();
    private readonly string directoryPath;
    private readonly int maxWaitMs;
    private readonly ILogger<FileUploadMetaHandler> logger;

    /// <summary>
    /// Instantiate a new object of <see cref="FileUploadMetaHandler"/>
    /// </summary>
    /// <param name="options">The options</param>
    /// <param name="logger">The optional logger</param>
    public FileUploadMetaHandler(
        IOptions<FileStorageOptions> options,
        ILogger<FileUploadMetaHandler>? logger = null
    )
    {
        directoryPath = options.Value.MetaDirectoryPath;
        maxWaitMs = options.Value.MaxWaitInMilliseconds;
        this.logger = logger ?? NullLogger<FileUploadMetaHandler>.Instance;
    }

    /// <inheritdoc />
    public Task<bool> CreateResourceAsync(UploadFileInfo fileInfo, CancellationToken cancellationToken)
    {
        try
        {
            if (!fileInfo.IsPartial)
            {
                return Task.FromResult(WriteUploadFileInfo(fileInfo));
            }
            else
            {
                return Task.FromResult(WritePartialUploadFileInfo(fileInfo, cancellationToken));
            }
        }
        catch (Exception)
        {
            // Oops
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public Task<UploadFileInfo?> GetResourceAsync(string fileId, CancellationToken cancellationToken)
    {
        var fileInfo = ReadUploadFileInfo(fileId);
        return Task.FromResult(fileInfo);
    }

    /// <inheritdoc />
    public Task<bool> UpdateResourceAsync(UploadFileInfo fileInfo, CancellationToken cancellationToken)
    {
        var filepath = MetadataFullFilenamePath(fileInfo.FileId);
        var exists = File.Exists(filepath);
        if (!exists)
        {
            return Task.FromResult(false);
        }

        if (!fileInfo.IsPartial)
        {
            return Task.FromResult(WriteUploadFileInfo(fileInfo));
        }
        else
        {
            return Task.FromResult(WritePartialUploadFileInfo(fileInfo, cancellationToken));
        }
    }

    /// <inheritdoc />
    public Task<bool> DeleteUploadFileInfoAsync(UploadFileInfo info, CancellationToken cancellationToken)
    {
        var path = "";
        if (!info.IsPartial)
        {
            path = MetadataFullFilenamePath(info.FileId);
        }
        else
        {
            path = MetadataPartialFilenamePath(info.PartialId);
        }
        var exists = File.Exists(path);
        if (!exists)
        {
            // Does not exists uhm so it is? deleted
            return Task.FromResult(true);
        }

        File.Delete(path);
        var deleted = !File.Exists(path);
        return Task.FromResult(deleted);
    }

    /// <inheritdoc />
    public async Task<UploadFileInfo?> GetPartialResourceAsync(string partialId, CancellationToken cancellationToken)
    {
        try
        {
            var file = MetadataPartialFilenamePath(partialId);

            var locked = Rwl.TryEnterReadLock(maxWaitMs);
            if (!locked)
            {
                return null;
            }

            try
            {
                // Read and deserialize the metadata file
                var fileInfoText = await File.ReadAllTextAsync(file, cancellationToken);
                return JsonSerializer.Deserialize<UploadFileInfo>(fileInfoText);
            }
            finally
            {
                Rwl.ExitReadLock();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not retrieve partial upload file info for partial id: {PartialID}", partialId);
            return null;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<UploadFileInfo> GetAllResourcesAsync()
    {
        var filenames = Directory.GetFiles(directoryPath, "*.metadata.json", SearchOption.AllDirectories);
        foreach (var filename in filenames)
        {
            UploadFileInfo? info = null;
            try
            {
                var text = await File.ReadAllTextAsync(filename);
                info = JsonSerializer.Deserialize<UploadFileInfo>(text);
            }
            catch (IOException) { }
            catch (JsonException) { }
            if (info is not null)
            {
                yield return info;
            }
        }
    }

    private bool WriteUploadFileInfo(UploadFileInfo fileInfo)
    {
        try
        {
            var filename = MetadataFullFilenamePath(fileInfo.FileId);
            var content = JsonSerializer.Serialize(fileInfo);
            File.WriteAllText(filename, content);
            return true;
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Could not write upload file info {@UploadFileInfo}", fileInfo);
            return false;
        }
    }

    private bool WritePartialUploadFileInfo(UploadFileInfo fileInfo, CancellationToken cancellationToken)
    {
        try
        {
            var locked = Rwl.TryEnterWriteLock(maxWaitMs);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (locked)
                {
                    var filename = MetadataPartialFilenamePath(fileInfo.PartialId);
                    var sysInfo = new FileInfo(filename);
                    sysInfo.Directory?.Create();
                    var content = JsonSerializer.Serialize(fileInfo);
                    File.WriteAllText(filename, content);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                Rwl.ExitWriteLock();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not write partial upload file info {@PartialUpload}", fileInfo);
        }

        return false;
    }

    private UploadFileInfo? ReadUploadFileInfo(string fileId)
    {
        var filename = MetadataFullFilenamePath(fileId);
        var exists = File.Exists(filename);
        if (!exists)
        {
            return null;
        }

        try
        {
            var fileInfoTxt = File.ReadAllText(filename);
            var fileInfo = JsonSerializer.Deserialize<UploadFileInfo>(fileInfoTxt);
            return fileInfo;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private string MetadataFullFilenamePath(string fileId) => Path.Combine(directoryPath, $"{fileId}.metadata.json");

    private string MetadataPartialFilenamePath(string partialId)
    {
        var temp = partialId.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries);
        var sanitized = string.Join("_", temp);
        var path = Path.Combine(directoryPath, sanitized, "chunk.metadata.json");
        return path;
    }
}

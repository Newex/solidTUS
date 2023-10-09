using System;
using System.Collections.Generic;
using System.IO;
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
    private readonly string directoryPath;
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
                return Task.FromResult(WritePartialUploadFileInfo(fileInfo));
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
        var filename = MetadataFullFilename(fileId);
        var path = Path.Combine(directoryPath, filename);
        var exists = File.Exists(path);
        if (!exists)
        {
            // Try partial
            filename = MetadataPartialFilename(fileId);
            path = Path.Combine(directoryPath, filename);
            exists = File.Exists(path);
            if (!exists)
            {
                return Task.FromResult<UploadFileInfo?>(null);
            }
        }

        try
        {
            var fileInfoTxt = File.ReadAllText(path);
            var fileInfo = JsonSerializer.Deserialize<UploadFileInfo>(fileInfoTxt);
            return Task.FromResult(fileInfo);
        }
        catch (JsonException)
        {
            return Task.FromResult<UploadFileInfo?>(null);
        }
    }

    /// <inheritdoc />
    public Task<bool> UpdateResourceAsync(UploadFileInfo fileInfo, CancellationToken cancellationToken)
    {
        if (!fileInfo.IsPartial)
        {
            var filename = MetadataFullFilename(fileInfo.FileId);
            var path = Path.Combine(directoryPath, filename);
            if (File.Exists(path))
            {
                return Task.FromResult(WriteUploadFileInfo(fileInfo));
            }
            return Task.FromResult(false);
        }
        else
        {
            var filename = MetadataPartialFilename(fileInfo.FileId);
            var path = Path.Combine(directoryPath, filename);
            if (File.Exists(path))
            {
                return Task.FromResult(WritePartialUploadFileInfo(fileInfo));
            }
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public Task<bool> DeleteUploadFileInfoAsync(UploadFileInfo info, CancellationToken cancellationToken)
    {
        string? filename;
        if (!info.IsPartial)
        {
            filename = MetadataFullFilename(info.FileId);
        }
        else
        {
            filename = MetadataPartialFilename(info.FileId);
        }
        var path = Path.Combine(directoryPath, filename);
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
            var filename = MetadataFullFilename(fileInfo.FileId);
            var path = Path.Combine(directoryPath, filename);
            var content = JsonSerializer.Serialize(fileInfo);
            File.WriteAllText(path, content);
            return true;
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Could not write upload file info {@UploadFileInfo}", fileInfo);
            return false;
        }
    }

    private bool WritePartialUploadFileInfo(UploadFileInfo fileInfo)
    {
        try
        {
            var filename = MetadataPartialFilename(fileInfo.FileId);
            var sysInfo = new FileInfo(filename);
            sysInfo.Directory?.Create();
            var path = Path.Combine(directoryPath, filename);
            var content = JsonSerializer.Serialize(fileInfo);
            File.WriteAllText(path, content);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not write partial upload file info {@PartialUpload}", fileInfo);
        }

        return false;
    }

    private string MetadataFullFilename(string fileId)
    {
        var temp = fileId.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries);
        var sanitized = string.Join("_", temp);
        var filename = sanitized + ".metadata.json";
        return filename;
    }

    private string MetadataPartialFilename(string partialId)
    {
        var temp = partialId.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries);
        var sanitized = string.Join("_", temp);
        var filename = sanitized + ".chunk.metadata.json";
        return filename;
    }
}

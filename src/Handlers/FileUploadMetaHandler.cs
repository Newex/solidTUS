using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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

    /// <summary>
    /// Instantiate a new object of <see cref="FileUploadMetaHandler"/>
    /// </summary>
    /// <param name="options">The options</param>
    public FileUploadMetaHandler(
        IOptions<FileStorageOptions> options
    )
    {
        directoryPath = options.Value.MetaDirectoryPath;
    }

    /// <inheritdoc />
    public Task<bool> CreateResourceAsync(string fileId, UploadFileInfo fileInfo, CancellationToken cancellationToken)
    {
        try
        {
            WriteUploadFileInfo(fileId, fileInfo);
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
        return Task.FromResult(fileInfo);
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

            WriteUploadFileInfo(fileId, updates);

            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task<bool> SetTotalUploadedBytesAsync(string fileId, long totalBytes)
    {
        var fileInfo = ReadUploadFileInfo(fileId);
        if (fileInfo is null)
        {
            return Task.FromResult(false);
        }

        var update = fileInfo with
        {
            ByteOffset = totalBytes
        };
        WriteUploadFileInfo(fileId, update);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> SetFilePathForUploadAsync(string fileId, string filePath)
    {
        var fileInfo = ReadUploadFileInfo(fileId);
        if (fileInfo is null)
        {
            return Task.FromResult(false);
        }

        var update = fileInfo with
        {
            FileDirectoryPath = filePath
        };
        WriteUploadFileInfo(fileId, update);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> DeleteUploadFileInfoAsync(string fileId, CancellationToken cancellationToken)
    {
        var path = MetadataFullFilenamePath(fileId);
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

    private void WriteUploadFileInfo(string fileId, UploadFileInfo fileInfo)
    {
        var filename = MetadataFullFilenamePath(fileId);
        var content = JsonSerializer.Serialize(fileInfo);
        File.WriteAllText(filename, content);
    }

    private UploadFileInfo? ReadUploadFileInfo(string fileId)
    {
        var filename = MetadataFullFilenamePath(fileId);
        var exists = File.Exists(filename);
        if (!exists)
        {
            return null;
        }

        var fileInfoTxt = File.ReadAllText(filename);
        var fileInfo = JsonSerializer.Deserialize<UploadFileInfo>(fileInfoTxt);
        return fileInfo!;
    }

    private string MetadataFullFilenamePath(string fileId) => Path.Combine(directoryPath, $"{fileId}.metadata.json");
}

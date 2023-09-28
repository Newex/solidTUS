using System;
using System.Collections.Generic;
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

        var written = WriteUploadFileInfo(fileInfo);
        return Task.FromResult(written);
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

    /// <inheritdoc />
    public async IAsyncEnumerable<UploadFileInfo> GetAllResourcesAsync()
    {
        var filenames = Directory.GetFiles(directoryPath, "*.metadata.json");
        foreach (var filename in filenames)
        {
            var text = await File.ReadAllTextAsync(filename);
            var info = JsonSerializer.Deserialize<UploadFileInfo>(text);
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
        catch (Exception)
        {
            return false;
        }
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
}

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
    public Task<UploadFileInfo?> GetResourceAsync(string fileId, CancellationToken cancellationToken)
    {
        var fileInfo = ReadUploadFileInfo(fileId);
        return Task.FromResult(fileInfo);
    }

    /// <inheritdoc />
    public Task<bool> UpdateResourceAsync(string fileId, UploadFileInfo fileInfo, CancellationToken cancellationToken)
    {
        var filepath = MetadataFullFilenamePath(fileId);
        var exists = File.Exists(filepath);
        if (!exists)
        {
            return Task.FromResult(false);
        }

        var written = WriteUploadFileInfo(fileId, fileInfo);
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

    private bool WriteUploadFileInfo(string fileId, UploadFileInfo fileInfo)
    {
        try
        {
            var filename = MetadataFullFilenamePath(fileId);
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

        var fileInfoTxt = File.ReadAllText(filename);
        var fileInfo = JsonSerializer.Deserialize<UploadFileInfo>(fileInfoTxt);
        return fileInfo!;
    }

    private string MetadataFullFilenamePath(string fileId) => Path.Combine(directoryPath, $"{fileId}.metadata.json");
}

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
    public ValueTask<bool> CreateResourceAsync(string fileId, UploadFileInfo fileInfo, CancellationToken cancellationToken)
    {
        try
        {
            WriteUploadFileInfo(fileId, fileInfo);
        }
        catch (Exception)
        {
            // Oops
            return new ValueTask<bool>(false);
        }

        return new ValueTask<bool>(true);
    }

    /// <inheritdoc />
    public ValueTask<UploadFileInfo?> GetUploadFileInfoAsync(string fileId, CancellationToken cancellationToken)
    {
        var fileInfo = ReadUploadFileInfo(fileId);
        return new ValueTask<UploadFileInfo?>(fileInfo);
    }

    /// <inheritdoc />
    public ValueTask<bool> SetFileSizeAsync(string fileId, long totalFileSize, CancellationToken cancellationToken)
    {
        var fileInfo = ReadUploadFileInfo(fileId);
        if (fileInfo is not null)
        {
            var updates = fileInfo with
            {
                FileSize = totalFileSize
            };

            WriteUploadFileInfo(fileId, updates);

            return new ValueTask<bool>(true);
        }

        return new ValueTask<bool>(false);
    }

    /// <inheritdoc />
    public ValueTask<bool> SetTotalUploadedBytesAsync(string fileId, long totalBytes)
    {
        var fileInfo = ReadUploadFileInfo(fileId);
        if (fileInfo is null)
        {
            return new ValueTask<bool>(false);
        }

        var update = fileInfo with
        {
            ByteOffset = totalBytes
        };
        WriteUploadFileInfo(fileId, update);
        return new ValueTask<bool>(true);
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
        var fileInfoTxt = File.ReadAllText(filename);
        var fileInfo = JsonSerializer.Deserialize<UploadFileInfo>(fileInfoTxt);
        return fileInfo!;
    }

    private string MetadataFullFilenamePath(string fileId) => Path.Combine(directoryPath, $"{fileId}.metadata.json");
}

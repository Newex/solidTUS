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
            path = MetadataPartialFilenamePath(info.PartialUrlPath ?? string.Empty, info.FileId);
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
    public async Task<UploadFileInfo?> GetPartialResourceAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            // Construct the directory path based on the URL
            var directory = MetadataPartialDirectoryPath(url);

            // Check if the directory exists
            if (!Directory.Exists(directory))
            {
                return null;
            }

            // Get all files in the directory with the ".chunk.metadata.json" extension
            var metadataFiles = Directory.GetFiles(directory, "*.chunk.metadata.json");

            // Check if there is exactly one metadata file in the directory
            if (metadataFiles.Length != 1)
            {
                return null;
            }

            var locked = Rwl.TryEnterReadLock(maxWaitMs);
            if (!locked)
            {
                return null;
            }

            try
            {
                // Read and deserialize the metadata file
                var fileInfoText = await File.ReadAllTextAsync(metadataFiles[0], cancellationToken);
                return JsonSerializer.Deserialize<UploadFileInfo>(fileInfoText);
            }
            finally
            {
                Rwl.ExitReadLock();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not retrieve partial upload file info for URL: {Url}", url);
            return null; // Return null if an error occurs
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
                    var filename = MetadataPartialFilenamePath(fileInfo.PartialUrlPath ?? string.Empty, fileInfo.FileId);
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

    private string MetadataPartialDirectoryPath(string url)
    {
        if (!url.StartsWith("/"))
        {
            throw new ArgumentException(url);
        }

        var queryIndex = url.LastIndexOf("?");
        if (queryIndex > 0)
        {
            // Cut the query off
            url = url[..queryIndex];
        }

        url = directoryPath + "/" + url;
        var segments = url.Split("/").Where(x => !string.IsNullOrWhiteSpace(x));
        return Path.Combine(segments.ToArray()[..^1]);
    }

    private string MetadataPartialFilenamePath(string url, string partialId)
    {
        var directory = MetadataPartialDirectoryPath(url);
        var path = Path.Combine(directory, $"{partialId}.chunk.metadata.json");
        return path;
    }
}

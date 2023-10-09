using System;
using System.Threading.Tasks;
using SolidTUS.Contexts;
using SolidTUS.Models;

namespace SolidTUS.Builders;

/// <summary>
/// Tus upload context builder
/// </summary>
public class TusUploadContextBuilder
{
    private readonly string fileId;

    private Func<UploadFileInfo, Task>? uploadFinishedCallback;

    internal TusUploadContextBuilder(string fileId)
    {
        this.fileId = fileId;
    }

    /// <summary>
    /// Set the callback for when an upload has finished
    /// </summary>
    /// <param name="callback">The callback</param>
    /// <returns>A tus upload context builder</returns>
    public TusUploadContextBuilder OnUploadFinished(Func<UploadFileInfo, Task> callback)
    {
        uploadFinishedCallback = callback;
        return this;
    }

    /// <summary>
    /// Build the upload context
    /// </summary>
    /// <returns>A tus upload context</returns>
    public TusUploadContext Build()
    {
        return new TusUploadContext(fileId, uploadFinishedCallback);
    }
}

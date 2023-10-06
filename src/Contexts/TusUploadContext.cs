using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using SolidTUS.Handlers;
using SolidTUS.Models;

namespace SolidTUS.Contexts;

/// <summary>
/// The upload context
/// </summary>
public sealed record class TusUploadContext
{
    internal TusUploadContext(
        string fileId,
        Func<UploadFileInfo, Task>? uploadFinishedCallback
    )
    {
        FileId = fileId;
        UploadFinishedCallback = uploadFinishedCallback;
    }

    /// <summary>
    /// Get the file id
    /// </summary>
    public string FileId { get; }

    /// <summary>
    /// Get the callback for when the upload has finished
    /// </summary>
    public Func<UploadFileInfo, Task>? UploadFinishedCallback { get; }
}

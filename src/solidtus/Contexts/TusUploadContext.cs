using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SolidTUS.Handlers;
using SolidTUS.Models;
using SolidTUS.ProtocolFlows;

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

    /// <summary>
    /// Start appending data from from the client
    /// </summary>
    /// <param name="context">The http context</param>
    /// <returns>An awaitable task</returns>
    public async ValueTask StartAppendDataAsync(HttpContext context)
    {
        if (context.RequestServices.GetService(typeof(UploadHandler)) is not UploadHandler uploadHandler
        || context.RequestServices.GetService(typeof(UploadFlow)) is not UploadFlow uploadFlow)
        {
            throw new InvalidOperationException("Remember to register SolidTUS on program startup");
        }

        if (context.Items[TusResult.Name] is not TusResult tusResult)
        {
            throw new InvalidOperationException("Can only use this method in conjuction with either endpoint filter or action filter.");
        }

        var cancel = context.RequestAborted;
        var upload = await uploadHandler.HandleUploadAsync(context.Request.BodyReader, this, tusResult, cancel);
        UploadFlow.PostUpload(upload);
    }
}

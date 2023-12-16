using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SolidTUS.Attributes;
using SolidTUS.Extensions;
using SolidTUS.Handlers;

namespace ViteDotnet.Controllers;

[ApiController]
public class UploadController : ControllerBase
{
    private readonly IUploadStorageHandler uploadStorageHandler;
    private readonly IUploadMetaHandler uploadMetaHandler;

    public UploadController(
        IUploadStorageHandler uploadStorageHandler,
        IUploadMetaHandler uploadMetaHandler
    )
    {
        this.uploadStorageHandler = uploadStorageHandler;
        this.uploadMetaHandler = uploadMetaHandler;
    }

    [TusCreation("/api/upload")]
    [RequestSizeLimit(5_000_000_000)]
    public async Task<ActionResult> CreateFile()
    {
        var metadata = HttpContext.TusMetadata();
        if (metadata is not null)
        {
            // Read Metadata
            var filename = metadata["name"];
            var mime = metadata["type"];
        }

        // Construct some unique file id
        var id = Guid.NewGuid().ToString("N");
        var partialId = id[..8];

        var configuration = HttpContext
            .TusCreation(id)
            .WithParallelUploads()
            .SetPartialId(partialId)
            .Done();

        // Accept creating upload and redirect to TusUpload
        var ctx = configuration.Build("fileId");
        await ctx.StartCreationAsync(HttpContext);

        // Converts a success to 201 created
        return Ok();
    }

    [TusUpload("{fileId}")]
    [RequestSizeLimit(5_000_000_000)]
    public async Task<ActionResult> UploadFile(string fileId)
    {
        var ctxBuilder = HttpContext.TusUpload(fileId);
        ctxBuilder.OnUploadFinished((info) =>
        {
            Console.WriteLine("Finished uploading: {0}", info.OnDiskFilename);
            return Task.CompletedTask;
        });
        var ctx = ctxBuilder.Build();
        await HttpContext.StartAppendDataAsync(ctx);

        // Must always return 204 on upload success with no Body content
        return NoContent();
    }

    // MUST have same route as the Upload route
    [TusDelete("{fileId}")]
    public async Task<ActionResult> DeleteFile(string fileId, CancellationToken cancellationToken)
    {
        // No questions asked - just delete
        var info = await uploadMetaHandler.GetResourceAsync(fileId, cancellationToken);

        if (info is null)
        {
            // Should respond with 404
            // or if we know this existed
            // then 410 Gone
            return NotFound();
        }

        if (info.Done)
        {
            // SPECS does not specify what to do when
            // the upload IS finished!

            // We say "no" by 403 forbidden
            return Forbid();
        }

        // Delete info and file respond 204
        await uploadStorageHandler.DeleteFileAsync(info, cancellationToken);
        await uploadMetaHandler.DeleteUploadFileInfoAsync(info, cancellationToken);
        return NoContent();
    }
}

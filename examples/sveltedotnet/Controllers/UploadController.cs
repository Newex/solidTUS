using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SolidTUS.Attributes;
using SolidTUS.Contexts;
using SolidTUS.Handlers;
using SolidTUS.Models;

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
    public async Task<ActionResult> CreateFile([FromServices] TusCreationContext context)
    {
        // Read Metadata
        var filename = context.UploadFileInfo.Metadata["name"];
        var mime = context.UploadFileInfo.Metadata["type"];

        // Construct some unique file id
        var id = Guid.NewGuid().ToString("N");
        var partialId = id[..8];

        context.SetUploadRouteValues(new { fileId = id });

        var parallel = context
            .SetupParallelUploads("/part/{partialId}")
            .SetParallelIdParameterNameInTemplate("partialId")
            .SetPartialId(partialId)
            .SetRouteValues(new { partialId, hello = "World" })
            .OnMergeHandler((files) => files.Count > 1);

        context.ApplyParallelUploadsConfiguration(parallel.Build());


        // Accept creating upload and redirect to TusUpload
        await context.StartCreationAsync(id);

        // Converts a success to 201 created
        return Ok();
    }

    [TusParallel("/part/{partialId}/{hello}")]
    public Task<ActionResult> ParallelUploads(string partialId, string hello, [FromServices] TusUploadContext context)
    {
        throw new NotImplementedException();
    }

    [TusUpload("{fileId}")]
    [RequestSizeLimit(5_000_000_000)]
    public async Task<ActionResult> UploadFile(string fileId, [FromServices] TusUploadContext context)
    {
        // Set callback before awaiting upload, otherwise the callback won't be called
        context.OnUploadFinished(async (file) =>
        {
            var filename = file.Metadata["name"];
            Console.WriteLine($"Uploaded file {filename} with file size {file.FileSize}");
            await Task.CompletedTask;
        });

        context.SetExpirationStrategy(ExpirationStrategy.SlidingExpiration, TimeSpan.FromSeconds(30));
        await context.StartAppendDataAsync(fileId);

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
        return NoContent();
    }
}

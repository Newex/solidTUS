using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SolidTUS.Attributes;
using SolidTUS.Contexts;
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

    [TusUpload("{fileId}")]
    [RequestSizeLimit(5_000_000_000)]
    public async Task<ActionResult> Upload(string fileId, [FromServices] TusUploadContext context)
    {
        // Do not await if you want the callback

        // Callback
        context.OnUploadFinished(async (file) =>
        {
            var filename = file.Metadata["name"];
            Console.WriteLine($"Uploaded file {filename} with file size {file.FileSize}");
            await Task.CompletedTask;
        });

        context.SetExpirationStrategy(SolidTUS.Models.ExpirationStrategy.SlidingExpiration, TimeSpan.FromSeconds(30));

        // Await after callback defined
        await context.StartAppendDataAsync(fileId);
        // await upload;

        // Must always return 204 on upload success with no Body content
        return NoContent();
    }

    [TusCreation("/api/upload")]
    public async Task<ActionResult> CreateFile([FromServices] TusCreationContext context)
    {
        // Read Metadata
        var filename = context.UploadFileInfo.Metadata["name"];
        var mime = context.UploadFileInfo.Metadata["type"];

        // Construct upload URL
        var id = Guid.NewGuid().ToString("N");
        var uploadTo = Url.Action(nameof(Upload), new { fileId = id }) ?? string.Empty;

        // Can define callback before starting upload (creation-with-upload)
        context.OnUploadFinished(async () =>
        {
            // only if the upload HAS data will this be called
            Console.WriteLine("Finished uploading this file: " + filename);
            await Task.CompletedTask;
        });

        // Start creation (IuploadStorageHandler.CreateResource())
        await context.StartCreationAsync(id, uploadTo);


        // Converts a success to 201 created
        return Ok();
    }

    // Must have same route as the Upload route
    [HttpDelete("{fileId}")]
    public async Task<ActionResult> DeleteUpload(string fileId, CancellationToken cancellationToken)
    {
        // No questions asked - just delete
        var info = await uploadMetaHandler.GetResourceAsync(fileId, cancellationToken);

        if (info is null)
        {
            // Should respond with 404
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
        await uploadStorageHandler.DeleteFileAsync(info);
        return NoContent();
    }
}

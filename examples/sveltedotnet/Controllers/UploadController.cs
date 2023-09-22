using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SolidTUS.Attributes;
using SolidTUS.Contexts;

namespace ViteDotnet.Controllers;

[ApiController]
public class UploadController : ControllerBase
{
    [Route("{fileId}")]
    [TusUpload]
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

    [HttpPost("/api/upload")]
    [TusCreation]
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

}

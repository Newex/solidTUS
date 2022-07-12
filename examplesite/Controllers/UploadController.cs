using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SolidTUS.Attributes;
using SolidTUS.Models;

namespace ExampleSite.Controllers;

[Route("upload")]
public class UploadController : ControllerBase
{
    [Route("{fileId}")]
    [TusUpload]
    [RequestSizeLimit(5_000_000_000)]
    public async Task<ActionResult> Upload(string fileId, TusUploadContext context)
    {
        // Starting append a.k.a. upload
        await context.StartAppendDataAsync(fileId);

        context.OnUploadFinished(async _ => await Task.CompletedTask);

        // Must always return 204 on upload success with no Body content
        return NoContent();
    }

    [TusCreation]
    public async Task<ActionResult> CreateFile(TusCreationContext context)
    {
        // Read Metadata
        var filename = context.Metadata["filename"];
        var mime = context.Metadata["contentType"];
        context.ActualFileName = filename;
        context.MimeType = mime;

        // Construct upload URL
        var id = Guid.NewGuid().ToString("N");
        var uploadTo = Url.Action(nameof(Upload), new { fileId = id }) ?? string.Empty;

        // Start creation (IuploadStorageHandler.CreateResource())
        await context.StartCreationAsync(id, uploadTo);

        // Converts a success to 201 created
        return Ok();
    }
}

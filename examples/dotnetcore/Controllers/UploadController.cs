using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SolidTUS.Attributes;
using SolidTUS.Contexts;
using SolidTUS.Handlers;

namespace ExampleSite.Controllers;

[Route("upload")]
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

    [TusCreation]
    public async Task<ActionResult> CreateFile([FromServices] TusCreationContextOLD context)
    {
        // Read Metadata
        var filename = context.UploadFileInfo.Metadata["filename"];
        var mime = context.UploadFileInfo.Metadata["contentType"];

        // Construct upload URL
        var id = Guid.NewGuid().ToString("N");

        context.SetUploadRouteValues(new { fileId = id }, "CustomRouteNameUpload");

        // Start creation (IuploadStorageHandler.CreateResource())
        await context.StartCreationAsync(id);

        // Converts a success to 201 created
        return Ok();
    }

    [TusUpload("{fileId}", Name = "CustomRouteNameUpload")]
    [RequestSizeLimit(5_000_000_000)]
    public async Task<ActionResult> Upload(string fileId, [FromServices] TusUploadContext context)
    {
        // Starting append a.k.a. upload
        // Can set path per file or use default from global configuration
        await context.StartAppendDataAsync(fileId);

        // context.OnUploadFinished(async _ => await Task.CompletedTask);
        // context.TerminateUpload(fileId);

        // Must always return 204 on upload success with no Body content
        return NoContent();
    }

    // Must have same route as the Upload route
    [TusDelete("{fileId}", UploadNameEndpoint = "CustomRouteNameUpload")]
    public async Task<ActionResult> DeleteUpload(string fileId, CancellationToken cancellationToken)
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

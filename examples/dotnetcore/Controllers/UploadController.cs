using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SolidTUS.Attributes;
using SolidTUS.Extensions;
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

    [HttpGet("{fileId}")]
    public async Task<IActionResult> Download(string fileId, CancellationToken cancellationToken)
    {
        var info = await uploadMetaHandler.GetResourceAsync(fileId, cancellationToken);
        if (info is null)
        {
            return NotFound();
        }

        if (info.OnDiskDirectoryPath is not null)
        {
            var file = Path.Combine(info.OnDiskDirectoryPath, info.OnDiskFilename);
            var stream = System.IO.File.OpenRead(file);
            if (stream is null)
            {
                return NotFound();
            }

            return File(stream, info.Metadata?["contentType"] ?? "application/octet-stream", info.Metadata?["filename"]);
        }

        return NotFound();
    }


    [TusCreation]
    [RequestSizeLimit(5_000_000_000)]
    public async Task<ActionResult> CreateFile()
    {
        var id = Guid.NewGuid().ToString("N");

        // Read Metadata
        var metadata = HttpContext.TusMetadata();
        if (metadata is not null)
        {
            var filename = metadata["filename"];
            var mime = metadata["contentType"];
            Console.WriteLine("Filename is: {0}\nMime-type is: {1}", filename, mime);
        }

        // Construct upload URL
        var ctx = HttpContext
            .TusCreation(id)
            .SetRouteName("CustomRouteNameUpload")
            .Build("{fileId}", "fileId");

        // Start creation (IuploadStorageHandler.CreateResource())
        await HttpContext.StartCreationAsync(ctx);

        // Converts a success to 201 created
        return Ok();
    }

    [TusUpload("{fileId}", Name = "CustomRouteNameUpload")]
    [RequestSizeLimit(5_000_000_000)]
    public async Task<ActionResult> Upload(string fileId)
    {
        var ctx = HttpContext
            .TusUpload(fileId)
            .Build();
        await HttpContext.StartAppendDataAsync(ctx);

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
        await uploadMetaHandler.DeleteUploadFileInfoAsync(info, cancellationToken);
        return NoContent();
    }
}

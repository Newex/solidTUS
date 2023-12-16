using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private readonly IEnumerable<EndpointDataSource> endpointsSources;
    private readonly IUploadStorageHandler uploadStorageHandler;
    private readonly IUploadMetaHandler uploadMetaHandler;

    public UploadController(
        IEnumerable<EndpointDataSource> endpointsSources,
        IUploadStorageHandler uploadStorageHandler,
        IUploadMetaHandler uploadMetaHandler
    )
    {
        this.endpointsSources = endpointsSources;
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
            .Build("fileId", ("name", "World"));

        // Start creation (IuploadStorageHandler.CreateResource())
        await ctx.StartCreationAsync(HttpContext);

        // Converts a success to 201 created
        return Ok();
    }

    [TusUpload("{fileId}/hello/{name}", Name = "CustomRouteNameUpload")]
    [RequestSizeLimit(5_000_000_000)]
    public async Task<ActionResult> Upload(string fileId, string name)
    {
        var ctx = HttpContext
            .TusUpload(fileId)
            .Build();
        await ctx.StartAppendDataAsync(HttpContext);

        // Must always return 204 on upload success with no Body content
        return NoContent();
    }

    [HttpGet("{fileId}/hello/{name}")]
    public async Task<ActionResult> Download(string fileId, string name, CancellationToken cancellationToken)
    {
        var meta = await uploadMetaHandler.GetResourceAsync(fileId, cancellationToken);
        if (meta is null)
        {
            return NotFound();
        }

        var filePath = Path.Combine(meta.OnDiskDirectoryPath ?? "./", meta.OnDiskFilename);
        if (filePath is null)
        {
            return NotFound();
        }

        var stream = System.IO.File.OpenRead(filePath);
        return File(stream, meta.Metadata?["contentType"] ?? "application/octet-stream", meta.Metadata?["filename"] ?? fileId, true);
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

    [HttpGet]
    public ActionResult GetEndpoints()
    {
        var endpoints = endpointsSources.SelectMany(es => es.Endpoints).OfType<RouteEndpoint>();
        var createRoute = "";
        var createName = "";
        var uploadRoute = "";
        var uploadName = "";
        foreach (var endpoint in endpoints)
        {
            if (endpoint.Metadata.OfType<TusCreationAttribute>().Any())
            {
                createRoute = endpoint.RoutePattern.RawText;
                createName = endpoint.Metadata.OfType<RouteNameMetadata>().FirstOrDefault()?.RouteName;
                // RouteNameMetadata
            }
            else if (endpoint.Metadata.OfType<TusUploadAttribute>().Any())
            {
                uploadRoute = endpoint.RoutePattern.RawText;
                uploadName = endpoint.Metadata.OfType<RouteNameMetadata>().FirstOrDefault()?.RouteName;
            }
        }

        var metadata = endpoints.Select(m => m.Metadata);
        var test = metadata.SelectMany(m => m);

        throw new NotImplementedException();
    }
}

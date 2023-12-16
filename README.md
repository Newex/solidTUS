![](https://badgen.net/nuget/v/SolidTus)
# What is it?
A C# dotnet 8 implementation of the [TUS-protocol](https://tus.io/protocols/resumable-upload.html#core-protocol) for resumable file uploads for an ASP.NET Core application.

**Why create another TUS library?**  
This library's purpose is to be simple and give the consumer more options on how to authorize and authenticate requests.  
Which I felt that other libraries did not provide.  
SolidTUS is a more Controller/Action oriented and integrates well with a Web API or an MVC project.

If you have any suggestions or improvements please do not hesitate to contribute.

### Current TUS features 
SolidTUS currently implements the following.

Basic features:
- [x] Core-protocol v.1.0.0 (stop and resume uploads)
- [x] Max-size (define a hard limit for upload size)
- [x] Tus-metadata validation
- [x] Options (server feature announcements)

Extensions:
- [x] [Creation](https://tus.io/protocols/resumable-upload#creation)
- [x] [Creation-With-Upload](https://tus.io/protocols/resumable-upload#creation-with-upload)
- [x] [Expiration](https://tus.io/protocols/resumable-upload#expiration)
- [x] [Concatenation](https://tus.io/protocols/resumable-upload#concatenation)
- [x] [Checksum](https://tus.io/protocols/resumable-upload#checksum) *
- [x] [Termination](https://tus.io/protocols/resumable-upload#termination) **

**Notes:**  
\* Checksum feature does not implement the trailing header feature, i.e. A checksum value must be provided upon sending the http request.  
\** Termination must be implemented by yourself. See examples and [documentation](https://github.com/Newex/solidTUS/wiki/TUS-Termination) on how to and why.

**Other:**  
The API is not stable, still trying to find an easy to use and understandable API - thus I might experiment and change usage.  
Versioning does not follow semantic versioning.

### Other TUS libraries for C#
* [tusdotnet](https://github.com/tusdotnet/tusdotnet)

# Quickstart

Add the package to your project:  
```console
$ dotnet add package SolidTUS
```

Register the service in the startup process:

```csharp
// Register TUS services
builder.Services.AddTUS();
```

## Using MVC
In your `Controller` add the `TusCreation`-attribute to the action method endpoint.

### Get upload info
```csharp
[TusCreation("/upload/{myArg}")]
[Microsoft.AspNetCore.Mvc.RequestSizeLimit(5_000_000_000)]
public async Task<ActionResult> CreateFileInfo(string myArg)
{
    // Create a globally unique file id string
    var myFileId = Guid.NewGuid().ToString("N");

    // Read Metadata
    var metadata = HttpContext.TusMetadata();
    if (metadata is not null)
    {
        // The client (uploader) sets the metadata
        // so they may not exist
        var filename = metadata["filename"];
        var mime = metadata["contentType"];
        Console.WriteLine("Filename is: {0}\nMime-type is: {1}", filename, mime);
    }

    // Construct upload URL
    TusCreationContext ctx = HttpContext
        .TusCreation(myFileId)
        .Build("fileId", ("name", "World"));
        // Must provide the name of the file id parameter
        // and any additional parameters for the endpoint

    // Note if the request contains upload data in the body
    // This will start the upload - normally upload data is
    // done on TusUpload endpoint.
    await ctx.StartCreationAsync(HttpContext);

    // Converts a success to 201 created
    // With a Location header to the upload URL
    return Ok();
}
```

This will not upload any file (unless the client explicitly uses the TUS-extension `Creation-With-Upload` feature).  
This only sets the ground work for getting information such as file size, and where to upload the data.

When building the `TusCreationContext ctx` the `Build` method must have the name of the parameter name for the file id.
Any extra arguments should be in the tuple format `("parameterName", "value")`.

The upload URL is constructed using these values to set the Location header, furthermore, these values will be used when decoding `Upload-Concat` requests from the client.

The `RequestSizeLimit` is the actual allowed upload size when using `Creation-With-Upload` TUS-extension, which is different from the `TusOptions.MaxSize`. The `TusOptions.MaxSize` checks the `Content-Length` header if it is within the limit.

### Next the upload

```csharp
[TusUpload("{fileId}/hello/{name}")]
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
```

_And done..._

Congratulations you now have a very basic upload / pause / resume functionality.

## Using minimal API
Add an endpoint to your minimal api:

```csharp
app.MapTusCreation("/upload/{myArg}", async (HttpContext context, string myArg) =>
{
    var myFileId = Guid.NewGuid().ToString("N");
    var create = context
        .TusCreation(myFileId)
        .Build("fileId", ("hello", "world"));

    await create.StartCreationAsync(context);

    // A success will be converted to 201
    // with a Location header pointing to the upload endpoint
    return Results.Ok();
});
```

Then define the upload endpoint:

```csharp
app.MapTusUpload("/upload/{fileId}/{hello}", async (HttpContext http, string fileId, string hello) =>
{
    var upload = http
        .TusUpload(fileId)
        .Build();
    await upload.StartAppendDataAsync(http);
    return Results.NoContent();
});
```

Important: The fileId in the `MapTusUpload` method assumes that the 2nd parameter is `fileId` (index = 1).  
To change it call the `MapTusUpload` method like so:

```csharp
app.MapTusUpload("/upload/{fileId}/{hello}", async (string fileId, [FromServices] HttpContext http, string hello) =>
{
    // ... Omitted
}, fileIdIndex: 0);
```

# Extra options
To see all the configurations go to the [wiki](https://github.com/Newex/solidTUS/wiki).
## Configurations

SolidTUS can be configured through the `TusOptions` object, either on startup or using environment variables.

```csharp
// Custom metadata provider or set maximum TUS protocol file size
builder.Services
  .AddTUS()
  .SetMetadataValidator(metadata => metadata.ContainsKey("filename"))
  .AllowEmptyMetadata(true)
  .Configuration(options =>
  {
    // This max size is different than the ASP.NET specified max size.
    // To change the request size limit do it per Action with an attribute (recommended).
    options.MaxSize = 5_000_000_000;
  });
```
All options are mentioned in the [wiki/tus-options](https://github.com/Newex/solidTUS/wiki/TusOptions)  
Note: to change request size limits see: [Microsoft documentation](https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-7.0#kestrel-maximum-request-body-size)

If you don't want to use the default FileUploadStorageHandler you can provide your own, maybe you want to save the files to a database?

```csharp
// Add custom storage handler
builder.Services
  .AddTUS()
  .AddStorageHandler<MyStorageHandler>(); // <-- must implement IUploadStorageHandler interface
```

If you use the default FileUploadStorageHandler you can configure the directory where to store files:

```csharp
// Add custom checksum validator
builder.Services
  .AddTUS()
  .FileStorageConfiguration(options =>
  {
    options.DirectoryPath = "path/to/where/save/upload/files";
  });
```

## Context Builders
The context builders are meant to be used in conjunction with the Tus attributes. These context builders are created using extension methods on the `HttpContext`.

So that `TusCreation` extension method should be used with the `TusCreation` attribute, etc.

Each context builder uses a fluent interface to construct a tus context to be used in the library.

## The attribute and their purpose
The `TusCreation` attribute is responsible for:

1. Creating new uploads, by creating the `UploadFileInfo` metadata resource
2. Merging finished partial uploads into a single file

The `TusUpload` attribute is responsible for:

1. Storing upload data from the client
2. Keeping the `UploadFileInfo` in-sync with upload

The `TusDelete` attribute is responsible for:

1. Terminating an upload
2. Must have the same endpoint route as `TusUpload`

# Limitations

The library current limitations, are:

- Cannot specify where the `UploadFileInfo` metadata should be stored on a per upload basis.

This is due to the SolidTUS `FileUploadMetaHandler` only searching for metadata in a specific directory.

# The TUS protocol with SolidTUS simplified
In essence the client sends a request to an endpoint as marked by the `TusCreation` attribute:

```
POST /files HTTP/1.1
Host: tus.example.org
Content-Length: 0
Upload-Length: 100
Tus-Resumable: 1.0.0
```

The server then knows to expect an upload file with a total size of 100 bytes and at which point SolidTUS creates an `UploadFileInfo` which contains this metadata.  
The server responds on success where the file can be uploaded:

```
HTTP/1.1 201 Created
Location: https://tus.example.org/files/24e533e02ec3bc40c387f1a0e460e216
Tus-Resumable: 1.0.0
```

In this example the file has an ID of 24e533e02ec3bc40c387f1a0e460e216.  
The client then uploads the data to that location as marked by the `TusUpload` attribute:

```
PATCH /files/24e533e02ec3bc40c387f1a0e460e216 HTTP/1.1
Host: tus.example.org
Content-Type: application/offset+octet-stream
Content-Length: 30
Upload-Offset: 70
Tus-Resumable: 1.0.0

[remaining 30 bytes]

```

After some while the upload starts from byte 70 and has a total size of 100, the missing 30 bytes are in the PATCH body. This data is directed to the `OnPartialUploadAsync` method in the `IUploadStorageHandler`.

When finished successfully the server responds:

```
HTTP/1.1 204 No Content
Tus-Resumable: 1.0.0
Upload-Offset: 100
```

# Test methodology
Using unit tests and manually making TUS-request with the official javascript client in the examples folder.

# TODO

- [ ] Create wiki pages for all the configuration options
- [ ] Create wiki pages for library design, and how to extend
- [ ] Add section in readme for examples
- [ ] Make `TusDeleteAttribute` automatically detect upload url template
- [ ] Add `TusDelete` minimal API extension

# References
* [TUS-protocol](https://tus.io/protocols/resumable-upload.html#core-protocol)

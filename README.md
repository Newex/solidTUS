# What is it?
A C# dotnet 6 implementation of the [TUS-protocol](https://tus.io/protocols/resumable-upload.html#core-protocol) for resumable file uploads for an ASP.NET Core application.

**Why create another TUS library?**  
This library's purpose is to be simple and give the consumer more options on how to authorize and authenticate requests.  
Which I felt that other libraries did not provide.  
SolidTUS is a more Controller/Action oriented and integrates well with a Web API or an MVC project.

If you have any suggestions or improvements please do not hesitate to contribute.


### Other TUS libraries for C#
* [tusdotnet](https://github.com/tusdotnet/tusdotnet)

# Install
Run `dotnet add package SolidTUS`

# Configuration
The required configuration is to register the services in the startup process:

```csharp
// Register TUS services
builder.Services.AddTUS();
```
# Usage
Then in the `Controller` you mark the 2 actions that will be the TUS file creation endpoint with a `TusCreation`-attribute and the upload action with a `TusUpload`-attribute:

Each `Action` will be injected with a context class: `TusCreationContext` and `TusUploadContext`.

```csharp
[TusCreation]
public async Task<ActionResult> CreateUpload(TusCreationContext context)
{
  // ... Construct a file ID and an URL route to the Upload endpoint
  var fileID = "myFileID";
  var url = "/url/path/to/upload/action/myFileID";
  
  // When ready to create resource metadata
  await context.StartCreationAsync(fileID, url);
  
  return NoContent();
}
```

For the upload action:

```csharp
[TusUpload]
[RequestSizeLimit(5_000_000_000)] // <-- example: Set upload size for the action to 5 Gib
[Route("url/path/to/upload/action/{fileId}")]
public async Task<ActionResult> Upload(string fileId, TusUploadContext context)
{
  // ... stuff
  await context.StartAppendDataAsync(fileId);
  
  // Important must return 204 on success (TUS-protocol)
  return NoContent();
}
```

# TUS-features
* Core-protocol v.1.0.0 (stop and resume uploads)
* Creation (create upload)
* Creation-with-upload (upload and create in a single step)
* Checksum (validate partial uploads with a given algorithm and checksum)
* Max-size (define a hard limit for upload size)
* Tus-metadata validation
* Options (server feature announcements)

Eventual future features (not implemented):
* Expiration (deletion of unfinished uploads)
* Concatenation (upload chunks and then combine the files)

Note that the checksum feature does not implement the trailing header feature, i.e. A checksum value must be provided upon sending the http request.

# Extra options
## Configurations

TUS configurations  

```csharp
// Custom metadata provider or set maximum TUS protocol file size
builder.Services
  .AddTUS()
  .Configuration(options =>
  {
    // A Func<string, bool> that validates the given TUS-metadata upon resource creation
    options.MetadataValidator = (metadata) => metadata.ContainsKey("filename");
    
    // This max size is different than the ASP.NET specified max size. To change the request size limit do it per Action with an attribute (recommended).
    options.MaxSize = 5_000_000_000;
  });
```
Note: to change request size limits see: [Microsoft documentation](https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-6.0#kestrel-maximum-request-body-size)

If you don't want to use the default FileUploadStorageHandler you can provide your own, maybe you want to save the files to a database?

```csharp
// Add custom storage handler
builder.Services
  .AddTUS()
  .AddStorageHandler<MyStorageHandler>(); // <-- must implement IUploadStorageHandler interface
```

If you want to support more checksum validators. The default checksum validators are: SHA1 and MD5:

```csharp
// Add custom checksum validator
builder.Services
  .AddTUS()
  .AddChecksumValidator<MyChecksumValidator>(); // <-- must implement IChecksumValidator interface
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

## Configuration from appSettings.json or environment variables
You can configure the `Tus-Max-Size` parameter and the default file storage upload folder from the appSettings.json configuration:

```json
{
  "SolidTUS": {
    "DirectoryPath": "/path/to/my/uploads",
    "MaxSize": "3000000"
  }
}
```

Environment variables are named as `SolidTUS__DirectoryPath` with a double underscore (so they also can be read from a linux environment). See [Microsofts documentation for naming](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-6.0#naming-of-environment-variables)

## Contexts
The injected context classes are excluded from ModelBinding but do show up in Swagger SwashBuckle. Excluding the contexts from the Swagger document, will be left as an exercise for the reader.

### TusUploadContext
Is responsible for starting or terminating the upload. A termination is a premature ending and signals to the client that the upload has been terminated.  
The class contains the following members:

* `UploadFileInfo` - Upload file metadata (file size, offset, TUS-metadata and id)
* `OnUploadFinished` - A method that takes an awaitable callback. When the whole file has been completely uploaded the callback is invoked.
* `StartAppendDataAsync` - Starts accepting the upload stream from the client
* `TerminateUpload` - Returns an error http response (default: 400 BadRequest)

MUST call either `StartAppendDataAsync` or `TerminateUpload` method. Cannot call both in a single request (you can't accept and not accept an upload).

The `TusUploadContext` is injected from the `TusUpload`-attribute.

### TusUploadAttribute
Is responsible for marking the TUS-protocol upload endpoint. And needs information about 2 things:

1. The file ID parameter name of type `string`
2. The context parameter name of type `TusUploadContext`

These parameters can be tuned:

```csharp
[TusUpload(FileIdParameterName = "Id", ContextParameterName = "tus")]
public async Task<ActionResult> UploadEndPoint(string Id, TusUploadContext tus)
{
  /* Logic omitted ... */
}
```

### TusCreationContext
Is responsible for creating the resource metadata `UploadFileInfo`. Defining the file ID and eventual any TUS-metadata.  
SolidTUS implements the TUS-protocol extension `creation-with-upload` thus a resource creation can contain some upload data.  
Before reaching the actual `Action` method the metadata validator defined in the `Configuration` will run; If metadata is invalid an automatic response of 400 Bad Request will be returned, as specified in the TUS-protocol.   
The class contains the following members:

* `StartCreationAsync` - Starts resource creation
* `OnResourceCreated` - A method that takes a callback function, which is invoked when the resource has been successfully created
* `OnUploadFinished` - A method that takes a callback function, which is invoked when the partial file or whole file has finished uploading. It could be the client has sent a partial upload or the whole file.
* `Metadata` - A `Dictionary<string, string>` property of the parsed TUS-metadata

The `TusCreationContext` is injected from the `TusCreation`-attribute.

### TusCreationAttribute
Is responsible for marking the TUS-creation endpoint and needs information about the context parameter name.

```csharp
[TusCreation(ContextParameterName = "creationContext")]
public async Task<ActionResult> CreationEndPoint(TusUploadContext creationContext)
{
  /* Logic omitted ... */
}
```

# The TUS protocol with SolidTUS simplified
In essence the client sends a request to an endpoint (as marked by the `TusCreation` attribute:

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

The upload starts from byte 70 and has a total size of 100, the missing 30 bytes are in the PATCH body. This data is directed to the `OnPartialUploadAsync` method in the `IUploadStorageHandler`.

On success the server responds:

```
HTTP/1.1 204 No Content
Tus-Resumable: 1.0.0
Upload-Offset: 100
```

# Test methodology
Using unit tests and manually making TUS-request with the official javascript client in the examples folder.

# Dependencies

This package uses the [C# functional extensions package](https://github.com/louthy/language-ext), which can be a very opinionated library.  
In the future I would like to not depend on this library so the consumer wouldn't have to deal with function pollutions.

# References
* [TUS-protocol](https://tus.io/protocols/resumable-upload.html#core-protocol)

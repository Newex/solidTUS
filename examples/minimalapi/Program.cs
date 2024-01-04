using SolidTUS.Extensions;
using SolidTUS.Handlers;

var builder = WebApplication.CreateSlimBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services
    .AddTus()
    .FileStorageConfiguration(options =>
    {
        options.DirectoryPath = "./Uploads";
        options.MetaDirectoryPath = "./Uploads";
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapTusCreation("/create", async (HttpContext context) =>
{
    var create = context
        .TusCreation("random_fileId")
        .Build("fileId", ("hello", "world"));

    await create.StartCreationAsync(context);
    return Results.Ok();
}, tags: "Create")
.WithOpenApi();

app.MapTusUpload("/upload/{fileId}/{hello}", async (HttpContext http, string fileId, string hello) =>
{
    var upload = http
        .TusUpload(fileId)
        .OnUploadFinished(info =>
        {
            Console.WriteLine($"Finished upload ${info.OnDiskFilename}");
            return Task.CompletedTask;
        })
        .Build();
    await upload.StartAppendDataAsync(http);
    return Results.NoContent();
}, tags: "Upload")
.WithTusDelete(
    async (IUploadMetaHandler meta, string fileId, IUploadStorageHandler upload, CancellationToken cancel) =>
    {
        var info = await meta.GetResourceAsync(fileId, cancel);
        if (info is null)
        {
            return Results.NotFound();
        }

        if (info.Done || info.IsPartial)
        {
            return Results.Forbid();
        }

        await upload.DeleteFileAsync(info, cancel);
        return Results.NoContent();
    });

app.Run();
using SolidTUS.Attributes;
using SolidTUS.Constants;
using SolidTUS.Extensions;
using SolidTUS.Models;

var builder = WebApplication.CreateSlimBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services
    .AddTus();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapTusCreation("/create", async (HttpContext context, LinkGenerator linkGenerator) =>
{
    var link = linkGenerator.GetPathByName(EndpointNames.UploadEndpoint);

    var tus = context
        .TusCreation("random_fileId")
        .Build("fileId", ("hello", "world"));

    await tus.StartCreationAsync(context);
    return Results.Ok();
});

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
    await http.StartAppendDataAsync(upload);
    return Results.NoContent();
});


// app
//     .MapGet(
//         "/message/{hello}",
//         (HttpContext ctx, string hello) =>
//         {
//             ctx.Response.Headers.Clear();
//             return Results.Ok();
//         }
//     )
//     .AddEndpointFilter(async (ctx, next) =>
//     {
//         var headers = ctx.HttpContext.Response.Headers;
//         var input = ctx.GetArgument<string>(1);
//         headers.TryAdd("MyHeaders", input);
//         await next(ctx);
//         return Results.NoContent();
//         // return await next(ctx);
//     });

app.Run();
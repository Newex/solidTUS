using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SolidTUS.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Can load from configuration
// builder.Services.AddTus(builder.Configuration);
builder.Services.AddTus()
    .Configuration(options =>
        options.MetadataValidator =
            (metadata) => metadata.ContainsKey("filename") && metadata.ContainsKey("contentType"))
    .FileStorageConfiguration(options =>
    {
        options.DirectoryPath = "/path/to/uploads";
        // options.MetaDirectoryPath = "/path/to/meta/info/file";
        options.MetaDirectoryPath = "/home/johnny/DigifySolutions/Projekter/Sanasa/projectweb/filetransfer/solidTUS/FILES";
    });

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.UseEndpoints(endpoints => endpoints.MapControllers());

app.Run();

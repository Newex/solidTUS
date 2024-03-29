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
builder.Services
    .AddTus(
        builder.Configuration,
        options => options.HasTermination = true)
    .SetMetadataValidator(m => m.ContainsKey("filename") && m.ContainsKey("contentType"))
    .FileStorageConfiguration(options =>
    {
        options.DirectoryPath = "./FILES";
        options.MetaDirectoryPath = "./FILES";
    });

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

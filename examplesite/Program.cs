using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SolidTUS.Extensions;
using SolidTUS.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTus()
    .Configuration(options =>
        options.MetadataValidator =
            (metadata) => metadata.ContainsKey("filename") && metadata.ContainsKey("contentType"))
    .FileStorageConfiguration(options =>
        options.DirectoryPath = "/path/to/folder");

builder.Services.AddControllers();
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("MyCorsPolicy", policy =>
//     {
//         policy.WithOrigins("https://localhost:7030/index.html");
//     });
// });

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

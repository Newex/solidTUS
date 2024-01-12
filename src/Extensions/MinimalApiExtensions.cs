using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SolidTUS.Constants;
using SolidTUS.Filters;
using SolidTUS.Models;

namespace SolidTUS.Extensions;

/// <summary>
/// Extension methods for minimal api web application
/// </summary>
public static class MinimalApiExtensions
{
    private static (string Key, OpenApiHeader Header) resumable = (TusHeaderNames.Resumable, new OpenApiHeader
    {
        Required = true,
        Description = TusHeaderValues.TusPreferredVersion,
        Schema = new()
        {
            Type = "string",
        }
    });
    private static (string Key, OpenApiHeader Header) uploadOffset = (TusHeaderNames.UploadOffset, new OpenApiHeader
    {
        Description = "Uploaded data in bytes",
        Schema = new()
        {
            Type = "number"
        }
    });
    private static (string Key, OpenApiHeader Header) uploadLength = (TusHeaderNames.UploadLength, new OpenApiHeader
    {
        Description = "The entire upload size in bytes",
        Schema = new()
        {
            Type = "number"
        }
    });
    private static (string Key, OpenApiHeader Header) cacheControl = ("Cache-Control", new OpenApiHeader
    {
        Description = "no-store",
        Schema = new()
        {
            Type = "string"
        }
    });

    private static readonly OpenApiParameter TusResumableHeaderParameter = new()
    {
        In = ParameterLocation.Header,
        Name = TusHeaderNames.Resumable,
        Example = new OpenApiString(TusHeaderValues.TusPreferredVersion),
        Description = "Tus version. Only 1.0.0 is supported.",
        Required = true,
        Schema = new()
        {
            Type = "string"
        }
    };

    /// <summary>
    /// Maps an endpoint for TUS file upload.
    /// </summary>
    /// <remarks>
    /// Route must include a file id parameter.
    /// It is assumed that the filterId is the 2nd argument (index = 1) in the delegate.
    /// </remarks>
    /// <param name="app">The web application</param>
    /// <param name="route">The route path</param>
    /// <param name="handler">The route handler</param>
    /// <param name="fileIdIndex">The fileId argument index</param>
    /// <param name="routeName">Optional route name</param>
    /// <param name="tags">The optional open api tags</param>
    /// <param name="requireAuth">True if HEAD, POST and PATCH should require authorization</param>
    /// <returns>A route handler builder</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Does not give warning in a minimal api.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Does not give warning in a minimal api.")]
    public static RouteHandlerBuilder MapTusUpload(this WebApplication app,
                                                   [StringSyntax("Route")] string route,
                                                   Delegate handler,
                                                   int fileIdIndex = 1,
                                                   string? routeName = null,
                                                   string? tags = null,
                                                   bool requireAuth = false)
    {
        var status = app
            .MapMethods(route, ["HEAD"], handler)
            .AddEndpointFilter(new TusStatusFilter(fileIdIndex))
            .WithDescription("Get the status of a Tus upload.")
            .Produces(200)
            .WithOpenApi(open =>
            {
                open.Parameters.Add(TusResumableHeaderParameter);

                var headers = open.Responses["200"].Headers;
                headers.Add(resumable.Key, resumable.Header);
                headers.Add(uploadOffset.Key, uploadOffset.Header);
                headers.Add(uploadLength.Key, uploadLength.Header);
                headers.Add(cacheControl.Key, cacheControl.Header);
                return open;
            });

        var bypass = app
            .MapPost(route, handler)
            .AddEndpointFilter(new TusUploadFilter(fileIdIndex))
            .WithDescription("Use X-Http-Method-Override request using POST to send as PATCH. If possible use PATCH. See PATCH for details.")
            .Produces(StatusCodes.Status204NoContent)
            .WithOpenApi(open =>
            {
                open.Parameters.Add(new()
                {
                    In = ParameterLocation.Header,
                    Name = "X-Http-Method-Override",
                    Example = new OpenApiString("PATCH"),
                    Description = "Send a PATCH request as a POST request.",
                    Required = true,
                    Schema = new()
                    {
                        Type = "string"
                    }
                });
                return open;
            });

        var upload = app
            .MapPatch(route, handler)
            .WithName(routeName ?? EndpointNames.UploadEndpoint)
            .WithMetadata(new SolidTusMetadataEndpoint(EndpointNames.UploadEndpoint, route, SolidTusEndpointType.Upload))
            .AddEndpointFilter(new TusUploadFilter(fileIdIndex))
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status410Gone)
            .Produces(StatusCodes.Status412PreconditionFailed)
            .Produces(StatusCodes.Status415UnsupportedMediaType)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithOpenApi(open =>
            {
                open.RequestBody = new OpenApiRequestBody()
                {
                    Required = true,
                    Description = "The upload body content.",
                    Content = new Dictionary<string, OpenApiMediaType>()
                    {
                        { TusHeaderValues.PatchContentType, new OpenApiMediaType()
                        {
                            Example = new OpenApiString("Content example here. Must match the Content-Length header in size."),
                            Schema = new()
                            {
                                Type = "file"
                            }
                        }}
                    }
                };

                open.Parameters.Add(TusResumableHeaderParameter);
                open.Parameters.Add(new()
                {
                    In = ParameterLocation.Header,
                    Name = "Content-Length",
                    Example = new OpenApiInteger(11),
                    Description = "The size of the current upload.",
                    Required = true,
                    Schema = new()
                    {
                        Type = "number"
                    }
                });
                open.Parameters.Add(new()
                {
                    In = ParameterLocation.Header,
                    Name = TusHeaderNames.UploadOffset,
                    Example = new OpenApiInteger(5),
                    Description = "The offset from which this upload continues.",
                    Required = true,
                    Schema = new()
                    {
                        Type = "number"
                    }
                });

                open.Responses["204"].Description = "No Content. Upload content successfully recieved.";
                open.Responses["409"].Description = "Conflict. Client and server Upload-Offset do not match.";
                open.Responses["410"].Description = "Gone. Upload resource has expired.";
                open.Responses["412"].Description = "Precondition Failed. The Tus-Resumable header version is not supported by the server or is missing.";
                open.Responses["415"].Description = "Unsupported Media Type. Must use the Content-Type header with application/offset+octet-stream.";

                var headers = open.Responses["204"].Headers;
                headers.Add(resumable.Key, resumable.Header);
                headers.Add(uploadOffset.Key, uploadOffset.Header);
                headers.Add(TusHeaderNames.UploadMetadata, new()
                {
                    Description = "The Tus Upload-Metadata echoed as provided during creation.",
                    Schema = new()
                    {
                        Type = "string"
                    }
                });
                return open;
            });

        if (!string.IsNullOrEmpty(tags))
        {
            status = status.WithTags(tags);
            bypass = bypass.WithTags(tags);
            upload = upload.WithTags(tags);
        }

        if (requireAuth)
        {
            status = status.RequireAuthorization();
            bypass = bypass.RequireAuthorization();
            upload = upload.RequireAuthorization();
        }

        return upload;
    }

    /// <summary>
    /// Maps an endpoint for TUS file creation.
    /// </summary>
    /// <param name="app">The web application</param>
    /// <param name="route">The route path</param>
    /// <param name="handler">The route handler</param>
    /// <param name="routeName">Optional route name</param>
    /// <param name="tags">The optional open api tags</param>
    /// <param name="requireAuth">True if both POST and OPTIONS should require authorization</param>
    /// <returns>A route handler builder</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Does not give warning in a minimal api.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Does not give warning in a minimal api.")]
    public static RouteHandlerBuilder MapTusCreation(this WebApplication app,
                                                        [StringSyntax("Route")] string route,
                                                        Delegate handler,
                                                        string? routeName = null,
                                                        string? tags = null,
                                                        bool requireAuth = false)
    {
        var status = app.MapMethods(route, ["OPTIONS"], handler)
            .WithDescription("Discover Tus-Server capabilities.")
            .AddEndpointFilter(new TusDiscoveryFilter())
            .Produces(StatusCodes.Status204NoContent)
            .WithOpenApi(open =>
            {
                var headers = open.Responses["204"].Headers;
                headers.Add(resumable.Key, resumable.Header);
                headers.Add(TusHeaderNames.Version, new()
                {
                    Required = true,
                    Description = TusHeaderValues.TusServerVersions,
                    Schema = new()
                    {
                        Type = "string",
                    }
                });
                headers.Add(TusHeaderNames.ChecksumAlgorithm, new()
                {
                    Required = true,
                    Description = "Comma-separated list of supported checksum algorithms.",
                    Schema = new()
                    {
                        Type = "string"
                    }
                });
                headers.Add(TusHeaderNames.Extension, new()
                {
                    Description = TusHeaderValues.TusSupportedExtensions,
                    Schema = new()
                    {
                        Type = "string"
                    }
                });
                return open;
            });

        var create = app
            .MapPost(route, handler)
            .WithName(routeName ?? EndpointNames.CreationEpoint)
            .WithDescription("Request creating a new Tus upload. If request is accepted you may upload using a PATCH request.")
            .WithMetadata(new SolidTusMetadataEndpoint(EndpointNames.CreationEpoint, route, SolidTusEndpointType.Create))
            .AddEndpointFilter(new TusCreationFilter())
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status412PreconditionFailed)
            .Produces(StatusCodes.Status413PayloadTooLarge)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithOpenApi(open =>
            {
                open.Parameters.Add(TusResumableHeaderParameter);
                open.Parameters.Add(new()
                {
                    Name = TusHeaderNames.UploadLength,
                    Required = true,
                    In = ParameterLocation.Header,
                    Description = "The total upload size.",
                    Example = new OpenApiInteger(11),
                    Schema = new()
                    {
                        Type = "number"
                    }
                });
                open.Parameters.Add(new()
                {
                    In = ParameterLocation.Header,
                    Name = "Content-Length",
                    Example = new OpenApiInteger(11),
                    Description = "The size of the current upload.",
                    Required = true,
                    Schema = new()
                    {
                        Type = "number"
                    }
                });
                open.Parameters.Add(new()
                {
                    In = ParameterLocation.Header,
                    Name = TusHeaderNames.UploadConcat,
                    Example = new OpenApiString("partial"),
                    Description = "Optional header if starting a parallel upload using the Concatenation Tus-Extension. Can be 'partial' or 'final'.",
                    Required = false,
                    Schema = new()
                    {
                        Type = "string"
                    }
                });
                open.Parameters.Add(new()
                {
                    In = ParameterLocation.Header,
                    Name = TusHeaderNames.UploadMetadata,
                    Example = new OpenApiString("filename d29ybGRfZG9taW5hdGlvbl9wbGFuLnBkZg==,is_confidential"),
                    Description = "Optional header containing Tus metadata encoded in a key value pair, where the value is in base64. Note the server may impose constraints on the metadata.",
                    Required = false,
                    Schema = new()
                    {
                        Type = "string"
                    }
                });

                open.RequestBody = new OpenApiRequestBody()
                {
                    Description = "The optional upload body content. Using the Creation-With-Upload Tus-Extension.",
                    Content = new Dictionary<string, OpenApiMediaType>()
                    {
                        { TusHeaderValues.PatchContentType, new OpenApiMediaType()
                        {
                            Example = new OpenApiString("Content example here. Must match the Content-Length header in size."),
                            Schema = new()
                            {
                                Type = "file"
                            }
                        }}
                    }
                };

                var headers = open.Responses["201"].Headers;
                headers.Add(resumable.Key, resumable.Header);
                headers.Add("Location", new()
                {
                    Description = "Location endpoint where to upload the data.",
                    Schema = new()
                    {
                        Type = "string",
                        Format = "uri"
                    }
                });

                open.Responses["412"].Description = "Precondition Failed. The Tus-Resumable header version is not supported by the server or is missing.";
                open.Responses["413"].Description = "Request Entity Too Large. The upload size exceeds the server specified Tus-Max-Size value.";

                return open;
            });

        if (!string.IsNullOrEmpty(tags))
        {
            status = status.WithTags(tags);
            create = create.WithTags(tags);
        }

        if (requireAuth)
        {
            status = status.RequireAuthorization();
            create = create.RequireAuthorization();
        }

        return create;
    }

    /// <summary>
    /// Maps an endpoint for the Tus-Termination. Must match the Tus upload route.
    /// </summary>
    /// <param name="app">The web application</param>
    /// <param name="route">The route pattern</param>
    /// <param name="handler">The termination handler</param>
    /// <returns>A route handler builder</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Does not give warning in a minimal api.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Does not give warning in a minimal api.")]
    public static RouteHandlerBuilder MapTusDelete(this WebApplication app,
                                                   [StringSyntax("Route")] string route,
                                                   Delegate handler)
    {
        return app
            .MapDelete(route, handler)
            .AddEndpointFilter<TusDeleteFilter>();
    }
}
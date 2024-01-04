using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using SolidTUS.Constants;
using SolidTUS.Extensions;
using SolidTUS.Models;

namespace SolidTUS.TusEndpoints;

/// <summary>
/// Upload endpoint route handler
/// </summary>
public sealed class UploadEndpointRouteHandlerBuilder : IEndpointConventionBuilder
{
    private readonly string routeTemplate;
    private RouteHandlerBuilder routeHandlerBuilder;
    private readonly WebApplication app;
    private readonly string? tags;

    internal UploadEndpointRouteHandlerBuilder(
        string routeTemplate,
        RouteHandlerBuilder routeHandlerBuilder,
        WebApplication app,
        string? tags
    )
    {
        this.routeTemplate = routeTemplate;
        this.routeHandlerBuilder = routeHandlerBuilder;
        this.app = app;
        this.tags = tags;
    }

    /// <inheritdoc />
    public void Add(Action<EndpointBuilder> convention)
    {
        routeHandlerBuilder.Add(convention);
    }

    /// <summary>
    /// Add TUS-termination endpoint.
    /// </summary>
    /// <param name="delete">The delete delegate</param>
    /// <returns>A route handler builder</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Does not give warning in a minimal api.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Does not give warning in a minimal api.")]
    public RouteHandlerBuilder WithTusDelete(Delegate delete)
    {
        routeHandlerBuilder = app
            .MapDelete(routeTemplate, delete)
            .AddEndpointFilter(async (context, next) =>
            {
                var tusResult = TusResult.Create(context.HttpContext.Request, context.HttpContext.Response);
                if (tusResult.TryGetError(out var error))
                {
                    context.HttpContext.SetErrorHeaders(error);
                    return error.ToResponseResult;
                }

                context.HttpContext.Response.OnStarting((state) =>
                {
                    var ctx = (HttpContext)state;
                    if (ctx.Response.StatusCode is >= 200 and < 300)
                    {
                        ctx.Response.StatusCode = 204;
                    }

                    return Task.CompletedTask;
                }, context.HttpContext);

                return await next(context);
            })
            .WithDescription("The Tus-Termination endpoint.")
            .Produces(StatusCodes.Status204NoContent)
            .WithOpenApi(open =>
            {
                open.Parameters.Add(new()
                {
                    Name = TusHeaderNames.Resumable,
                    In = ParameterLocation.Header,
                    Required = true,
                    Description = TusHeaderValues.TusPreferredVersion,
                    Schema = new()
                    {
                        Type = "string"
                    }
                });
                open.Responses["204"].Description = "No Content. Upload resource deleted.";
                return open;
            });

        if (!string.IsNullOrEmpty(tags))
        {
            routeHandlerBuilder.WithTags(tags);
        }

        return routeHandlerBuilder;
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using SolidTUS.Constants;
using SolidTUS.Models;
using SolidTUS.ProtocolFlows;
using SolidTUS.ProtocolHandlers;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Routing;
using Endpoints = System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Routing.EndpointDataSource>;

using static Microsoft.AspNetCore.Http.HttpMethods;
using SolidTUS.Functional.Models;
namespace SolidTUS.Attributes;

/// <summary>
/// Identifies an action that supports TUS resource creation.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class TusCreationAttribute : ActionFilterAttribute, IActionHttpMethodProvider, IRouteTemplateProvider
{
    /// <summary>
    /// Instantiate a new object of <see cref="TusCreationAttribute"/>
    /// </summary>
    public TusCreationAttribute()
    {
    }

    /// <summary>
    /// Instantiate a new <see cref="TusCreationAttribute"/> creation endpoint handler.
    /// </summary>
    /// <param name="template">The route template</param>
    public TusCreationAttribute([StringSyntax("Route")] string template)
    {
        ArgumentNullException.ThrowIfNull(template);
        Template = template;
    }

    /// <summary>
    /// Gets the supported http methods
    /// </summary>
    public IEnumerable<string> HttpMethods => new List<string>()
    {
        "POST", "OPTIONS"
    };

    /// <inheritdoc />
    public string? Template { get; init; }

    /// <inheritdoc />
    int? IRouteTemplateProvider.Order => Order;

    /// <inheritdoc />
    public string? Name { get; set; } = EndpointNames.CreationEpoint;

    /// <inheritdoc />
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var request = http.Request;

        var isPost = IsPost(request.Method);
        var isOptions = IsOptions(request.Method);

        if (!isPost && !isOptions)
        {
            await next();
            return;
        }

        var response = http.Response;
        if (isOptions)
        {
            var optionsHandler = http.RequestServices.GetService<OptionsRequestHandler>();
            if (optionsHandler is null)
            {
                throw new InvalidOperationException("Must register SolidTus on startup to use the functionalities");
            }

            optionsHandler.ServerFeatureAnnouncements(response.Headers);
            response.StatusCode = 204;
            return;
        }

        if (isPost)
        {
            var creationFlow = http.RequestServices.GetService<CreationFlow>();
            if (creationFlow is null)
            {
                throw new InvalidOperationException("Must register SolidTus on startup to use the functionalities");
            }

            if (http.RequestServices.GetService<Endpoints>() is not Endpoints endpointSources)
            {
                throw new UnreachableException();
            }

            var endpoints = endpointSources.SelectMany(es => es.Endpoints).OfType<RouteEndpoint>();
            string? uploadRoute = null;
            string? uploadName = null;
            foreach (var endpoint in endpoints)
            {
                if (endpoint.Metadata.OfType<TusUploadAttribute>().Any())
                {
                    uploadRoute = endpoint.RoutePattern.RawText;
                    uploadName = endpoint.Metadata.OfType<RouteNameMetadata>().FirstOrDefault()?.RouteName;
                }
            }

            if (string.IsNullOrWhiteSpace(uploadRoute))
            {
                // TODO: Consider mix matching attribute + filters
                // Minimal + Controllers
                throw new InvalidOperationException("Must have a TUS upload endpoint route");
            }

            var result = TusResult
                .Create(request, response)
                .Map(c => c with
                {
                    UploadRouteName = uploadName,
                    UploadRouteTemplate = uploadRoute
                });
            result = result.Bind(creationFlow.PreResourceCreation);
            var (isSuccess, ctx, error) = result;
            if (!isSuccess)
            {
                context.Result = new ObjectResult(error.Message)
                {
                    StatusCode = error.StatusCode
                };
                return;
            }

            context.HttpContext.Items[TusResult.Name] = ctx;
        }

        await next();
    }

    /// <inheritdoc />
    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        // Before sending headers -->
        context.HttpContext.Response.OnStarting(state =>
        {
            var ctx = (ResultExecutingContext)state;
            var isPost = IsPost(ctx.HttpContext.Request.Method);
            var response = ctx.HttpContext.Response;
            var isSuccess = response.StatusCode >= 200 && response.StatusCode < 300;
            if (isPost && isSuccess)
            {
                ctx.HttpContext.Response.StatusCode = 201;
            }
            return Task.CompletedTask;
        }, context);

        await next();
    }
}

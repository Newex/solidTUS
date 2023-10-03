using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using SolidTUS.Constants;
using SolidTUS.Extensions;
using SolidTUS.Models;
using SolidTUS.ProtocolFlows;
using SolidTUS.ProtocolHandlers;
using static Microsoft.AspNetCore.Http.HttpMethods;

namespace SolidTUS.Attributes;

/// <summary>
/// Identifies an action that supports TUS resource creation
/// </summary>
public class TusCreationAttribute : ActionFilterAttribute, IActionHttpMethodProvider, IRouteTemplateProvider
{
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
                response.StatusCode = 500;
                return;
            }

            var discoveryResponse = optionsHandler.ServerFeatureAnnouncements();
            response.AddTusHeaders(discoveryResponse);
            response.StatusCode = 204;
            return;
        }

        if (isPost)
        {
            var creationFlow = http.RequestServices.GetService<CreationFlow>();
            if (creationFlow is null)
            {
                context.Result = new ObjectResult("Internal server error")
                {
                    StatusCode = 500
                };
                return;
            }

            var requestContext = RequestContext.Create(request);
            requestContext = requestContext.Bind(creationFlow.PreResourceCreation);
            var error = requestContext.GetHttpError();
            if (error is not null)
            {
                context.Result = new ObjectResult(error.Value.Message)
                {
                    StatusCode = error.Value.StatusCode
                };
                return;
            }
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

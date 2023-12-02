using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SolidTUS.Constants;
using SolidTUS.Models;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;

namespace SolidTUS.Attributes;

/// <summary>
/// Marks the TUS-termination action. The route MUST match the route to the TUS-upload endpoint.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class TusDeleteAttribute : ActionFilterAttribute, IActionHttpMethodProvider, IRouteTemplateProvider
{
    /// <summary>
    /// Instantiate a new object of <see cref="TusDeleteAttribute"/>
    /// </summary>
    public TusDeleteAttribute()
    {
    }

    /// <summary>
    /// Instantiate a new object of <see cref="TusDeleteAttribute"/>
    /// </summary>
    /// <param name="template">The route template</param>
    /// <remarks>
    /// The route template must match the TUS-upload endpoint.
    /// </remarks>
    public TusDeleteAttribute([StringSyntax("Route")] string template)
    {
        ArgumentNullException.ThrowIfNull(template);
        Template = template;
    }

    /// <inheritdoc />
    public IEnumerable<string> HttpMethods => new List<string>
    {
        "DELETE"
    };

    /// <inheritdoc />
    public string? Template { get; init; }

    /// <inheritdoc />
    public string? Name { get; set; } = EndpointNames.TerminationEndpoint;

    /// <inheritdoc />
    int? IRouteTemplateProvider.Order => Order;

    /// <summary>
    /// The name of the route to the upload endpoint
    /// </summary>
    /// <remarks>
    /// Default name is "SolidTusUploadEndpoint".
    /// </remarks>
    public string? UploadNameEndpoint { get; set; } = EndpointNames.UploadEndpoint;

    /// <inheritdoc />
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var cancel = context.HttpContext.RequestAborted;
        var requestContext = TusResult.Create(context.HttpContext.Request, context.HttpContext.Response);
        var terminateRequest = context.HttpContext.RequestServices.GetService<TerminationRequestHandler>();
        if (terminateRequest is null)
        {
            context.Result = new ObjectResult("Error")
            {
                StatusCode = 500
            };
            return;
        }

        var values = context.RouteData.Values.AsEnumerable().Where(x => x.Key != "action" && x.Key != "controller");
        var routeData = new RouteValueDictionary(values);
        requestContext = requestContext.Bind(c => terminateRequest.ValidateRoute(c, Name, UploadNameEndpoint, routeData));
        if (requestContext.TryGetError(out var error))
        {
            context.Result = new ObjectResult(error.Message)
            {
                StatusCode = error.StatusCode
            };
            return;
        }

        await next();
    }

    /// <inheritdoc />
    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var cancel = context.HttpContext.RequestAborted;
        var requestContext = TusResult.Create(context.HttpContext.Request, context.HttpContext.Response);
        context.HttpContext.Response.OnStarting(state =>
        {
            var ctx = (ResultExecutingContext)state;
            var status = ctx.HttpContext.Response.StatusCode;
            if (requestContext.TryGetError(out var error))
            {
                ctx.HttpContext.Response.StatusCode = 204;
                return Task.CompletedTask;
            }

            ctx.Result = new ObjectResult(error.Message)
            {
                StatusCode = error.StatusCode
            };
            return Task.CompletedTask;
        }, context);

        await next();
    }
}

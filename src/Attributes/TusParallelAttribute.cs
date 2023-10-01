using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SolidTUS.Constants;
using SolidTUS.Extensions;
using SolidTUS.Models;
using SolidTUS.ProtocolFlows;

using static Microsoft.AspNetCore.Http.HttpMethods;

namespace SolidTUS.Attributes;

/// <summary>
/// Identifies an action that supports TUS parallel uploads. Must have a partial ID parameter and TusContext parameter.
/// </summary>
public class TusParallelAttribute : ActionFilterAttribute, IActionHttpMethodProvider, IRouteTemplateProvider
{
    /// <summary>
    /// Instantiate a new <see cref="TusParallelAttribute"/> partial upload endpoint handler
    /// </summary>
    /// <param name="template">The route template</param>
    public TusParallelAttribute([StringSyntax("Route")] string template)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(template);
        Template = template;
    }

    /// <inheritdoc />
    public string? Template { get; init; }

    /// <inheritdoc />
    public string? Name { get; set; } = EndpointNames.ParallelEndpoint;

    /// <summary>
    /// Gets the supported http methods
    /// </summary>
    public IEnumerable<string> HttpMethods => new List<string>()
    {
        "PATCH", "POST"
    };

    /// <inheritdoc />
    int? IRouteTemplateProvider.Order => Order;

    /// <summary>
    /// Gets or sets the name of the partial Id parameter
    /// </summary>
    public string PartialIdParameterName { get; set; } = ParameterNames.ParallelPartialIdParameterName;

    /// <summary>
    /// Gets or sets the name of the TUS context parameter
    /// </summary>
    public virtual string ContextParameterName { get; set; } = ParameterNames.TusUploadContextParameterName;

    /// <inheritdoc />
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var request = http.Request;
        var isPatch = IsPatch(request.Method) || (IsPost(request.Method) && IsPatch(request.Headers[TusHeaderNames.HttpMethodOverride]!));
        if (!isPatch)
        {
            // Skip to next
            await next();
            return;
        }

        var uploadFlow = http.RequestServices.GetService<UploadFlow>();
        if (uploadFlow is null)
        {
            context.Result = new ObjectResult("Internal server error")
            {
                StatusCode = 500
            };
            return;
        }

        var response = http.Response;
        var cancel = context.HttpContext.RequestAborted;
        var partialId = http.GetRouteValue(PartialIdParameterName)?.ToString() ?? string.Empty;
        var requestContext = RequestContext.Create(request, cancel);
        requestContext = await requestContext.BindAsync(async c => await uploadFlow.PreUploadAsync(c, partialId));
        var preCheck = requestContext.GetTusHttpResponse();
        if (!preCheck.IsSuccess)
        {
            response.AddTusHeaders(preCheck);
            context.Result = new ObjectResult(preCheck.Message)
            {
                StatusCode = preCheck.StatusCode
            };
            return;
        }

        var tusContext = uploadFlow.CreateUploadContext(requestContext, request.BodyReader, cancel);
        context.ActionArguments[ContextParameterName] = tusContext;

        // Callback before sending headers add all TUS headers
        context.HttpContext.Response.OnStarting(state =>
        {
            var ctx = (ActionExecutingContext)state;

            if (uploadFlow is not null)
            {
                var tusResponse = requestContext.GetTusHttpResponse(204);
                ctx.HttpContext.Response.AddTusHeaders(tusResponse);
            }

            return Task.CompletedTask;
        }, context);

        await next();
    }
}

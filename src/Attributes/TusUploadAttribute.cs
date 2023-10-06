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
/// Identifies an action that supports TUS uploads. Must have a file ID parameter and TusContext parameter.
/// </summary>
public class TusUploadAttribute : ActionFilterAttribute, IActionHttpMethodProvider, IRouteTemplateProvider
{
    /// <summary>
    /// Instantiate a new <see cref="TusUploadAttribute"/> upload endpoint handler
    /// </summary>
    /// <param name="template">The route template</param>
    public TusUploadAttribute([StringSyntax("Route")] string template)
    {
        ArgumentNullException.ThrowIfNull(template);
        Template = template;
    }

    /// <summary>
    /// Gets the supported http metods
    /// </summary>
    public IEnumerable<string> HttpMethods => new List<string>
    {
        "HEAD", "PATCH", "POST"
    };

    /// <inheritdoc />
    public string? Template { get; init; }

    /// <inheritdoc />
    public string? Name { get; set; } = EndpointNames.UploadEndpoint;

    /// <summary>
    /// The file id parameter name
    /// </summary>
    public string FileIdParameterName = ParameterNames.FileIdParameterName;

    /// <inheritdoc />
    int? IRouteTemplateProvider.Order => Order;

    /// <inheritdoc />
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var request = http.Request;

        var isHead = IsHead(request.Method);
        var isPatch = IsPatch(request.Method) || (IsPost(request.Method) && IsPatch(request.Headers[TusHeaderNames.HttpMethodOverride]!));

        if (!isHead && !isPatch)
        {
            // Skip to next
            await next();
            return;
        }

        var response = http.Response;
        var cancel = context.HttpContext.RequestAborted;

        var uploadFlow = http.RequestServices.GetService<UploadFlow>();
        if (uploadFlow is null)
        {
            context.Result = new ObjectResult("Internal server error")
            {
                StatusCode = 500
            };
            return;
        }

        var fileId = http.GetRouteValue(FileIdParameterName)?.ToString() ?? string.Empty;
        var tusResult = TusResult.Create(request, response);

        if (isHead)
        {
            tusResult = await tusResult.BindAsync(async c => await uploadFlow.GetUploadStatusAsync(c, fileId, cancel));
            var error = tusResult.GetHttpError();
            if (error is not null)
            {
                context.Result = new ObjectResult(error.Value.Message)
                {
                    StatusCode = error.Value.StatusCode
                };
                return;
            }

            return;
        }

        if (isPatch)
        {
            tusResult = await tusResult.BindAsync(async c => await uploadFlow.PreUploadAsync(c, fileId, cancel));
            var error = tusResult.GetHttpError();
            if (error is not null)
            {
                // Short circuit on error
                context.Result = new ObjectResult(error.Value.Message)
                {
                    StatusCode = error.Value.StatusCode
                };
                return;
            }

            var actual = tusResult.GetValueOrDefault();
            context.HttpContext.Items[TusResult.Name] = actual;

            // Callback before sending headers add all TUS headers
            context.HttpContext.Response.OnStarting(state =>
            {
                var ctx = (ActionExecutingContext)state;
                var uploadFlow = http.RequestServices.GetService<UploadFlow>();
                if (uploadFlow is null)
                {
                    ctx.Result = new ObjectResult("Internal server error")
                    {
                        StatusCode = 500
                    };
                    return Task.CompletedTask;
                }

                if (ctx.HttpContext.Items[HttpContextExtensions.UploadResultName] is not TusResult postAction)
                {
                    ctx.Result = new ObjectResult("Internal server error")
                    {
                        StatusCode = 500
                    };
                    return Task.CompletedTask;
                }

                uploadFlow.PostUpload(postAction);
                return Task.CompletedTask;
            }, context);
        }

        // Consider try catch ?! to move the
        await next();
    }

    /// <inheritdoc />
    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        // Force response to 204 on success
        context.HttpContext.Response.OnStarting(state =>
        {
            var ctx = (ResultExecutingContext)state;
            var httpMethodOverride = ctx.HttpContext.Request.Headers["X-HTTP-Method-Override"];
            var isPatch = IsPatch(ctx.HttpContext.Request.Method) || IsPatch(httpMethodOverride!);
            var response = ctx.HttpContext.Response;
            var isSuccess = response.StatusCode >= 200 && response.StatusCode < 300;
            if (isPatch && isSuccess)
            {
                ctx.HttpContext.Response.StatusCode = 204;
            }
            return Task.CompletedTask;
        }, context);

        await next();
    }
}
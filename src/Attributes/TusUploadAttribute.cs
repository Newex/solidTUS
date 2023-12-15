using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
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
/// Identifies an action that supports TUS uploads.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
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
        var fileId = http.GetRouteValue(FileIdParameterName)?.ToString() ?? string.Empty;

        if (context.HttpContext.RequestServices.GetService<UploadFlow>() is not UploadFlow uploadFlow)
        {
            context.Result = new ObjectResult("Internal server error")
            {
                StatusCode = 500
            };
            return;
        }

        var isHead = IsHead(http.Request.Method);
        var isPatch = IsPatch(http.Request.Method) || (IsPost(http.Request.Method) && IsPatch(http.Request.Headers[TusHeaderNames.HttpMethodOverride]!));

        var tusResult = TusResult.Create(context.HttpContext.Request, context.HttpContext.Response);
        var cancel = context.HttpContext.RequestAborted;
        if (isHead)
        {
            var status = await tusResult.Bind(async c => await uploadFlow.GetUploadStatusAsync(c, fileId, cancel));
            var (statusSuccess, statusFailure, statusResult, statusError) = status;
            var headers = statusSuccess ? statusResult.ResponseHeaders : statusError.Headers;
            foreach (var (key, value) in headers)
            {
                context.HttpContext.Response.Headers.Append(key, value);
            }

            context.HttpContext.Response.StatusCode = statusSuccess ? 200 : statusError.StatusCode;
            return;
        }

        if (!isPatch)
        {
            context.Result = new ObjectResult("Must be a PATCH or X-HTTP-Method-Override request to upload to a TUS endpoint")
            {
                StatusCode = 400
            };
            return;
        }

        tusResult = await tusResult.Bind(async c => await uploadFlow.PreUploadAsync(c, fileId, cancel));
        var (isSuccess, isFailure, tus, error) = tusResult;
        if (isFailure)
        {
            http.SetErrorHeaders(error);
            context.Result = new ObjectResult(error.Message)
            {
                StatusCode = error.StatusCode
            };
            return;
        }

        context.HttpContext.Items[TusResult.Name] = tus;
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
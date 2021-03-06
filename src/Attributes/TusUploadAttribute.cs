using System;
using System.Collections.Generic;
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
public class TusUploadAttribute : ActionFilterAttribute, IActionHttpMethodProvider
{
    private TusUploadContext? tusContext;

    /// <summary>
    /// Get or set the name of the file ID parameter
    /// </summary>
    public virtual string FileIdParameterName { get; set; } = "fileId";

    /// <summary>
    /// Get or set the name of the TUS context parameter
    /// </summary>
    public virtual string ContextParameterName { get; set; } = "context";

    /// <summary>
    /// Gets the supported http metods
    /// </summary>
    public IEnumerable<string> HttpMethods => new List<string>
    {
        "HEAD", "PATCH"
    };

    /// <inheritdoc />
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var request = http.Request;

        var isHead = IsHead(request.Method);
        var isPatch = IsPatch(request.Method) || IsPatch(request.Headers[TusHeaderNames.HttpMethodOverride]);

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
            response.StatusCode = 500;
            return;
        }

        var fileId = http.GetRouteValue(FileIdParameterName)?.ToString() ?? string.Empty;
        var requestContext = RequestContext.Create(request, cancel);

        if (isHead)
        {
            var uploadStatus = await requestContext.BindAsync(async c => await uploadFlow.GetUploadStatusAsync(c, fileId));
            var statusResponse = uploadStatus.GetTusHttpResponse(204);
            response.AddTusHeaders(statusResponse);
            context.Result = new ObjectResult(statusResponse.Message)
            {
                StatusCode = statusResponse.StatusCode
            };
            return;
        }

        if (isPatch)
        {
            var coreProtocolUpload = await requestContext.BindAsync(async c => await uploadFlow.StartUploadingAsync(c, fileId));
            var checksumExtension = coreProtocolUpload.Bind(c => uploadFlow.ChecksumFlow(c));
            var uploadResponse = checksumExtension.GetTusHttpResponse();
            if (!uploadResponse.IsSuccess)
            {
                // Short circuit on error
                response.AddTusHeaders(uploadResponse);
                context.Result = new ObjectResult(uploadResponse.Message)
                {
                    StatusCode = uploadResponse.StatusCode
                };
                return;
            }

            var finishedUpload = (long s) => response.Headers.Add(TusHeaderNames.UploadOffset, s.ToString());
            var onError = (HttpError error) =>
            {
                context.Result = new ObjectResult(error.Message)
                {
                    StatusCode = error.StatusCode
                };
            };

            tusContext = uploadFlow.CreateUploadContext(checksumExtension, request.BodyReader, finishedUpload, onError, cancel);
            context.ActionArguments[ContextParameterName] = tusContext;
        }

        await next();
    }

    /// <inheritdoc />
    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        // Before result
        // Before sending headers -->
        context.HttpContext.Response.OnStarting(state =>
        {
            var ctx = (ResultExecutingContext)state;
            var httpMethodOverride = ctx.HttpContext.Request.Headers["X-HTTP-Method-Override"];
            var isPatch = IsPatch(ctx.HttpContext.Request.Method) || IsPatch(httpMethodOverride);
            var response = ctx.HttpContext.Response;
            var isSuccess = response.StatusCode >= 200 && response.StatusCode < 300;
            if (isPatch && isSuccess)
            {
                ctx.HttpContext.Response.StatusCode = 204;
            }
            return Task.CompletedTask;
        }, context);

        await next();

        if (tusContext?.UploadHasBeenCalled == false)
        {
            throw new InvalidOperationException($"Remember to call {nameof(TusUploadContext.StartAppendDataAsync)}");
        }
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using SolidTUS.Constants;
using SolidTUS.Contexts;
using SolidTUS.Extensions;
using SolidTUS.Models;
using SolidTUS.ProtocolFlows;
using SolidTUS.ProtocolHandlers;
using static Microsoft.AspNetCore.Http.HttpMethods;

namespace SolidTUS.Attributes;

/// <summary>
/// Identifies an action that supports TUS resource creation
/// </summary>
public class TusCreationAttribute : ActionFilterAttribute, IActionHttpMethodProvider
{
    private TusCreationContext? tusContext;

    /// <summary>
    /// Get or set the name of the TUS context parameter
    /// </summary>
    public virtual string ContextParameterName { get; set; } = "context";

    /// <summary>
    /// Gets the supported http methods
    /// </summary>
    public IEnumerable<string> HttpMethods => new List<string>()
    {
        "POST", "OPTIONS"
    };

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
                response.StatusCode = 500;
                return;
            }

            var cancel = http.RequestAborted;
            var requestContext = RequestContext.Create(request, cancel);
            var startCreation = requestContext.Bind(creationFlow.StartResourceCreation);
            var creationResponse = startCreation.GetTusHttpResponse();
            if (!creationResponse.IsSuccess)
            {
                response.AddTusHeaders(creationResponse);
                context.Result = new ObjectResult(creationResponse.Message)
                {
                    StatusCode = creationResponse.StatusCode
                };
                return;
            }

            // Callbacks
            void CreatedResource(string location) => response.Headers.Add(HeaderNames.Location, location);
            void PartialUpload(long written) => response.Headers.Add(TusHeaderNames.UploadOffset, written.ToString());

            tusContext = creationFlow.CreateTusContext(
                startCreation,
                request.BodyReader,
                CreatedResource,
                PartialUpload,
                cancel
            );
            context.ActionArguments[ContextParameterName] = tusContext;
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
            var httpMethodOverride = ctx.HttpContext.Request.Headers["X-HTTP-Method-Override"];
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

        // Response probably already sent here
    }
}

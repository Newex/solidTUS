using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
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

            var result = TusResult.Create(request, response);
            result = result.Bind(creationFlow.PreResourceCreation);
            if (result.TryGetError(out var error))
            {
                context.Result = new ObjectResult(error.Message)
                {
                    StatusCode = error.StatusCode
                };
                return;
            }

            result.TryGetValue(out var ctx);
            context.HttpContext.Items[TusResult.Name] = ctx;
        }

        response.OnStarting(state =>
        {
            var ctx = (ActionExecutingContext)state;
            if (isPost)
            {
                if (ctx.HttpContext.Items[HttpContextExtensions.CreationResultName] is not Result<TusResult, HttpError> postAction)
                {
                    ctx.Result = new ObjectResult("Internal server error")
                    {
                        StatusCode = 500
                    };
                    return Task.CompletedTask;
                }

                if (postAction.TryGetError(out var error))
                {
                    ctx.Result = new ObjectResult(error.Message)
                    {
                        StatusCode = error.StatusCode
                    };
                    return Task.CompletedTask;
                }

                var creationFlow = http.RequestServices.GetService<CreationFlow>();
                if (creationFlow is null)
                {
                    ctx.Result = new ObjectResult("Internal server error")
                    {
                        StatusCode = 500
                    };
                    return Task.CompletedTask;
                }

                var postCreation = postAction.Map(creationFlow.PostResourceCreation);
                if (postCreation.TryGetError(out var postError))
                {
                    ctx.Result = new ObjectResult(postError.Message)
                    {
                        StatusCode = postError.StatusCode
                    };
                    return Task.CompletedTask;
                }
            }

            return Task.CompletedTask;
        }, context);

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

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SolidTUS.ProtocolHandlers;

using static Microsoft.AspNetCore.Http.HttpMethods;

namespace SolidTUS.Filters;

internal class TusDiscoveryFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        if (!IsOptions(http.Request.Method))
        {
            return await next(context);
        }

        var optionsHandler = http.RequestServices.GetService<OptionsRequestHandler>();
        if (optionsHandler is null)
        {
            throw new InvalidOperationException("Must register SolidTus on startup to use the functionalities");
        }

        optionsHandler.ServerFeatureAnnouncements(http.Response.Headers);
        return Results.NoContent();
    }
}

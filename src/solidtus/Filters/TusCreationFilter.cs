using System;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SolidTUS.Extensions;
using SolidTUS.Models;
using SolidTUS.ProtocolFlows;

using static Microsoft.AspNetCore.Http.HttpMethods;
using Endpoints = System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Routing.EndpointDataSource>;

namespace SolidTUS.Filters;

internal class TusCreationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var isPost = IsPost(http.Request.Method);
        if (!isPost)
        {
            return Results.BadRequest("Must be a POST request to create upload to a TUS endpoint");
        }

        if (http.RequestServices.GetService<CreationFlow>() is not CreationFlow creationFlow)
        {
            return Results.StatusCode(500);
        }

        if (http.RequestServices.GetService<Endpoints>() is not Endpoints endpoints)
        {
            return Results.StatusCode(500);
        }

        var metadataEndpoint = endpoints
            .SelectMany(es => es.Endpoints)
            .Select(e => e.Metadata)
            .SelectMany(m => m)
            .OfType<SolidTusMetadataEndpoint>()
            .SingleOrDefault(m => m.EndpointType == SolidTusEndpointType.Upload);

        if (metadataEndpoint is null)
        {
            // TODO: Also search for an attribute based upload route maybe?!
            // Must have upload endpoint!
            return Results.StatusCode(500);
        }

        var tusResult = TusResult
            .Create(http.Request, http.Response)
            .Map(c => c with
            {
                UploadRouteName = metadataEndpoint.Name,
                UploadRouteTemplate = metadataEndpoint.Route
            })
            .Bind(creationFlow.PreResourceCreation);

        var (isSuccess, isFailure, tus, error) = tusResult;
        if (isFailure)
        {
            http.SetErrorHeaders(error);
            return error.ToResponseResult;
        }

        http.Items[TusResult.Name] = tus;
        http.Response.OnStarting(CheckResponse, http);

        return await next(context);
    }

    private static Task CheckResponse(object state)
    {
        var ctx = (HttpContext)state;
        if (ctx.Response.StatusCode is not 201 and >= 200 and < 300)
        {
            ctx.Response.StatusCode = 201;
            if (ctx.Response.Headers.Location.Count == 0)
            {
                throw new InvalidOperationException("Must set Location header for created TUS resource");
            }
        }

        return Task.CompletedTask;
    }
}

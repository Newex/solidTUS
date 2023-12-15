using System;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SolidTUS.Models;
using SolidTUS.ProtocolFlows;

using static Microsoft.AspNetCore.Http.HttpMethods;

namespace SolidTUS.Filters;

internal class TusStatusFilter : IEndpointFilter
{
    private readonly int index;


    public TusStatusFilter(int index)
    {
        this.index = index;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var isHead = IsHead(http.Request.Method);

        if (!isHead)
        {
            return await next(context);
        }

        if (context.HttpContext.RequestServices.GetService<UploadFlow>() is not UploadFlow uploadFlow)
        {
            return Results.StatusCode(500);
        }

        var fileId = context.GetArgument<string>(index);
        var tusResult = await TusResult
            .Create(http.Request, http.Response)
            .Bind(async c => await uploadFlow.GetUploadStatusAsync(c, fileId, http.RequestAborted));

        var (isSuccess, isFailure, status, error) = tusResult;
        var headers = isSuccess ? status.ResponseHeaders : error.Headers;
        foreach (var (key, value) in headers)
        {
            context.HttpContext.Response.Headers.Append(key, value);
        }

        return isSuccess
            ? Results.Ok()
            : error.ToResponseResult;
    }

}

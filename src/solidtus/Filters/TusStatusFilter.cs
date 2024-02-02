using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SolidTUS.Extensions;
using SolidTUS.Models;
using SolidTUS.Models.Functional;
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

        var (isSuccess, status, error) = tusResult;
        if (!isSuccess)
        {
            http.SetErrorHeaders(error);
        }

        return isSuccess
            ? Results.Ok()
            : error.ToResponseResult;
    }
}

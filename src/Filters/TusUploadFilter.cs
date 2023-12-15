using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SolidTUS.Models;
using SolidTUS.ProtocolFlows;

namespace SolidTUS.Filters;

internal class TusUploadFilter : IEndpointFilter
{
    private readonly int index;

    public TusUploadFilter(int index)
    {
        this.index = index;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var fileId = context.GetArgument<string>(index);
        var http = context.HttpContext;
        if (context.HttpContext.RequestServices.GetService<UploadFlow>() is not UploadFlow uploadFlow)
        {
            return Results.StatusCode(500);
        }

        var tusResult = await TusResult
            .Create(context.HttpContext.Request, context.HttpContext.Response)
            .Bind(async c => await uploadFlow.PreUploadAsync(c, fileId, context.HttpContext.RequestAborted));
        context.HttpContext.Items[TusResult.Name] = tusResult;
        var result = await next(context);

        // Force to 204 no content or keep error
        return http.Response.StatusCode is not 204 and >= 200 and < 300
            ? Results.NoContent()
            : result;
    }
}
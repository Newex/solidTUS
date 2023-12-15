using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SolidTUS.Constants;
using SolidTUS.Extensions;
using SolidTUS.Models;
using SolidTUS.ProtocolFlows;
using static Microsoft.AspNetCore.Http.HttpMethods;

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
        var http = context.HttpContext;
        var isPatch = IsPatch(http.Request.Method) || (IsPost(http.Request.Method) && IsPatch(http.Request.Headers[TusHeaderNames.HttpMethodOverride]!));
        if (!isPatch)
        {
            return Results.BadRequest("Must be a PATCH or X-HTTP-Method-Override request to upload to a TUS endpoint");
        }

        var fileId = context.GetArgument<string>(index);
        if (context.HttpContext.RequestServices.GetService<UploadFlow>() is not UploadFlow uploadFlow)
        {
            return Results.StatusCode(500);
        }

        var tusResult = await TusResult
            .Create(context.HttpContext.Request, context.HttpContext.Response)
            .Bind(async c => await uploadFlow.PreUploadAsync(c, fileId, context.HttpContext.RequestAborted));
        var (isSuccess, isFailure, tus, error) = tusResult;
        if (isFailure)
        {
            http.SetErrorHeaders(error);
            return error.ToResponseResult;
        }

        context.HttpContext.Items[TusResult.Name] = tus;
        var result = await next(context);

        // Force to 204 no content or keep error
        return http.Response.StatusCode is not 204 and >= 200 and < 300
            ? Results.NoContent()
            : result;
    }
}
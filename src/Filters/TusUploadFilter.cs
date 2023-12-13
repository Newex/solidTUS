using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SolidTUS.Extensions;
using SolidTUS.Pipelines;
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
        var uploadFlow = context.HttpContext.RequestServices.GetService<UploadFlow>();
        var upload = await UploadPipeline.Begin(http, fileId, uploadFlow);
        if (upload.TryGetValue(out var error))
        {
            http.AddHeaderErrors(error);
            return error.ToResponseResult;
        }

        var result = await next(context);

        // Force to 204 no content or keep error
        return http.Response.StatusCode is not 204 and >= 200 and < 300
            ? Results.NoContent()
            : result;
    }
}
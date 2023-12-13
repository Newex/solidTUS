using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SolidTUS.Extensions;
using SolidTUS.Pipelines;

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
        var pre = await UploadPipeline.PreUpload(http, fileId);
        if (pre.TryGetValue(out var error))
        {
            http.AddHeaderErrors(error);
            return error.ToResponseResult;
        }

        http.Response.OnStarting(UploadPipeline.SetHeadersCallback, http);

        var result = await next(context);

        // Force to 204 no content or keep error
        return http.Response.StatusCode is not 204 and >= 200 and < 300
            ? Results.NoContent()
            : result;
    }
}
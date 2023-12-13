using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SolidTUS.Extensions;
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
        var pre = await UploadLogic.Pre(http, fileId);
        if (pre.TryGetValue(out var error))
        {
            http.AddHeaderErrors(error);
            return error.ToResponseResult;
        }

        http.Response.OnStarting(UploadLogic.SetHeadersCallback, http);

        var result = await next(context);

        // Force to 204 no content or keep error
        return http.Response.StatusCode is not 204 and >= 200 and < 300
            ? Results.NoContent()
            : result;
    }

}
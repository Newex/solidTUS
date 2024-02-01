using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SolidTUS.Extensions;
using SolidTUS.Functional.Models;
using SolidTUS.Models;
using SolidTUS.ProtocolHandlers;
using static Microsoft.AspNetCore.Http.HttpMethods;

namespace SolidTUS.Filters;

/// <summary>
/// The Tus-Delete termination endpoint filter.
/// Forces success status to be 204.
/// Checks if Tus-Resumable header is set.
/// </summary>
internal class TusDeleteFilter : IEndpointFilter
{
    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var isDelete = IsDelete(http.Request.Method);
        if (!isDelete)
        {
            return Results.BadRequest("Must be a DELETE request");
        }

        var tusResult = TusResult
            .Create(http.Request, http.Response)
            .Map(CommonRequestHandler.SetTusResumableHeader);

        var (isSuccess, _, error) = tusResult;
        if (!isSuccess)
        {
            http.SetErrorHeaders(error);
            return error.ToResponseResult;
        }

        http.Response.OnStarting((state) =>
        {
            var ctx = (HttpContext)state;
            if (ctx.Response.StatusCode is >= 200 and < 300)
            {
                ctx.Response.StatusCode = 204;
            }

            return Task.CompletedTask;
        }, http);

        return await next(context);
    }
}

using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SolidTUS.Constants;
using SolidTUS.Extensions;
using SolidTUS.Models;
using SolidTUS.ProtocolFlows;

using static Microsoft.AspNetCore.Http.HttpMethods;

namespace SolidTUS.Pipelines;

internal static class UploadPipeline
{
    public static async ValueTask<Maybe<HttpError>> PreUpload(HttpContext context, string? fileId)
    {
        var request = context.Request;
        var isHead = IsHead(request.Method);
        var isPatch = IsPatch(request.Method) || (IsPost(request.Method) && IsPatch(request.Headers[TusHeaderNames.HttpMethodOverride]!));

        if (!isHead && !isPatch)
        {
            return Maybe<HttpError>.None;
        }

        var response = context.Response;
        var cancel = context.RequestAborted;

        var uploadFlow = context.RequestServices.GetService<UploadFlow>();
        if (uploadFlow is null || fileId is null)
        {
            return HttpError.InternalServerError("Internal server error");
        }

        var tusResult = TusResult.Create(request, response);
        if (isHead)
        {
            tusResult = await tusResult.Bind(async c => await uploadFlow.GetUploadStatusAsync(c, fileId, cancel));
            return tusResult.Match(_ => Maybe<HttpError>.None, e => e);
        }

        if (isPatch)
        {
            tusResult = await tusResult.Bind(async c => await uploadFlow.PreUploadAsync(c, fileId, cancel));
            tusResult.Deconstruct(out var isSuccess, out var isFailure, out var result, out var error);
            if (isFailure)
            {
                return error;
            }

            context.Items[TusResult.Name] = result;
        }

        return Maybe<HttpError>.None;
    }

    public static Task SetHeadersCallback(object? state)
    {
        var ctx = (HttpContext)state!;
        var post = Post(ctx);
        if (post.TryGetError(out var error))
        {
            ctx.AddHeaderErrors(error);
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }

    private static Result<TusResult, HttpError> Post(HttpContext context)
    {
        if (context.Items[HttpContextExtensions.UploadResultName] is not Result<TusResult, HttpError> post)
        {
            return HttpError.InternalServerError();
        }

        post.Deconstruct(out var _, out var isFailure, out var result, out var postError);

        if (isFailure)
        {
            return postError;
        }

        var uploadFlow = context.RequestServices.GetRequiredService<UploadFlow>();
        return uploadFlow is null
            ? HttpError.InternalServerError()
            : uploadFlow.PostUpload(result);
    }
}

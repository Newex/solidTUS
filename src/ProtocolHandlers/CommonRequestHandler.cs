using System;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using SolidTUS.Constants;
using SolidTUS.Handlers;
using SolidTUS.Models;
using SolidTUS.Validators;
using static SolidTUS.Extensions.FunctionalExtensions;

namespace SolidTUS.ProtocolHandlers;

/// <summary>
/// General TUS request handler
/// </summary>
public class CommonRequestHandler
{
    private readonly IUploadStorageHandler uploadStorageHandler;

    /// <summary>
    /// Instantiate a new object of <see cref="CommonRequestHandler"/>
    /// </summary>
    /// <param name="uploadStorageHandler"></param>
    public CommonRequestHandler(
        IUploadStorageHandler uploadStorageHandler
    )
    {
        this.uploadStorageHandler = uploadStorageHandler;
    }

    /// <summary>
    /// Check if a <see cref="UploadFileInfo"/> resource has been created
    /// </summary>
    /// <remarks>
    /// Sets the <see cref="RequestContext.UploadFileInfo"/> if exists
    /// </remarks>
    /// <param name="context">The request context</param>
    /// <returns>Either an error or a request context</returns>
    public async Task<Either<HttpError, RequestContext>> CheckUploadFileInfoExistsAsync(RequestContext context)
    {
        var fileInfo = Optional(await uploadStorageHandler.GetUploadFileInfoAsync(context.FileID, context.CancellationToken));
        return match(fileInfo,
            Some: info => Either.Right(context with
            {
                UploadFileInfo = info
            }),
            None: () => HttpError.NotFound("File resource does not exists")
        );
    }

    /// <summary>
    /// Create TUS response
    /// </summary>
    /// <param name="request">The http request</param>
    /// <returns>A TUS http response</returns>
    [Obsolete("Delete me")]
    public static TusHttpResponse CreateResponse(HttpRequest request)
    {
        var result = new TusHttpResponse();
        var isOptions = request.Method == "OPTIONS";
        if (isOptions)
        {
            // Nothing -> delegate this to OPTIONS request handler
            result.IsSuccess = true;
            return result;
        }

        var isSupported = TusVersionValidator.IsValidVersion(request.Headers[TusHeaderNames.Resumable]);
        if (!isSupported)
        {
            result.IsSuccess = false;
            result.StatusCode = 412;
            result.Headers.Add(TusHeaderNames.Version, TusHeaderValues.TusServerVersions);
            return result;
        }

        result.IsSuccess = true;
        result.Headers.Add(TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion);
        return result;
    }

    /// <summary>
    /// Check if the TUS version is supported by the server
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>Either an http error or a request context</returns>
    public static Either<HttpError, RequestContext> CheckTusVersion(RequestContext context)
    {
        if (context.Method == "OPTIONS")
        {
            return context;
        }

        var isSupported = TusVersionValidator.IsValidVersion(context.RequestHeaders[TusHeaderNames.Resumable]);
        if (!isSupported)
        {
            var error = HttpError.PreconditionFailed();

            error.Headers.Add(TusHeaderNames.Version, TusHeaderValues.TusServerVersions);
            return error;
        }

        return context;
    }
}
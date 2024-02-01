using Microsoft.AspNetCore.Http;
using SolidTUS.Constants;
using SolidTUS.Functional.Models;
using SolidTUS.Models;
using SolidTUS.Tests.Mocks;

namespace SolidTUS.Tests.ProtocolHandlerTests.POST;

internal static class Setup
{
    public static Result<TusResult, HttpError> CreateRequest(bool resumable = true, params (string, string)[] header)
    {
        var http = MockHttps.HttpRequest("POST",
            header
        );

        if (resumable)
        {
            http.Headers.Append(TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion);
        }

        var response = MockHttps.HttpResponse();
        return TusResult.Create(http, response);
    }
}

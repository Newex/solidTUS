using System.Threading;
using SolidTUS.Constants;
using SolidTUS.Models;
using SolidTUS.Tests.Mocks;

namespace SolidTUS.Tests.ProtocolHandlerTests.POST;

internal static class Setup
{
    public static Result<TusResult> CreateRequest(bool resumable = true, params (string, string)[] header)
    {
        var http = MockHttps.HttpRequest("POST",
            header
        );

        if (resumable)
        {
            http.Headers.Add(TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion);
        }

        return TusResult.Create(http.HttpContext.Request, http.HttpContext.Response);
    }
}

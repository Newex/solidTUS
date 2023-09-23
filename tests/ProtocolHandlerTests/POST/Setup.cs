using System.Threading;
using SolidTUS.Constants;
using SolidTUS.Models;
using SolidTUS.Tests.Mocks;

namespace SolidTUS.Tests.ProtocolHandlerTests.POST;

public static class Setup
{
    public static Result<RequestContext> CreateRequest(bool resumable = true, params (string, string)[] header)
    {
        var http = MockHttps.HttpRequest("POST",
            header
        );

        if (resumable)
        {
            http.Headers.Add(TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion);
        }

        return RequestContext.Create(http, CancellationToken.None);
    }
}

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Moq;
using SolidTUS.Constants;

namespace SolidTUS.Tests.Mocks;

public static class MockHttps
{
    /// <summary>
    /// Default http request with TUS-Resumable 1.0.0 header if nothing specified
    /// </summary>
    /// <param name="method">The request method</param>
    /// <param name="headers">The request headers</param>
    /// <returns>A mock http request</returns>
    public static HttpRequest HttpRequest(string method = "GET", params (string Key, string Value)[] header)
    {
        var mock = new Mock<HttpRequest>();

        mock.Setup(r => r.Method)
        .Returns(method);

        mock.Setup(r => r.Headers)
        .Returns(new Dictionary<string, string>().ToHeaderDictionary(header));

        return mock.Object;
    }

    public static IHeaderDictionary Headers(params (string Key, string Value)[] header)
    {
        var headers = new HeaderDictionary();
        foreach (var (key, value) in header)
        {
            headers.TryAdd(key, value);
        }
        return headers;
    }

    private static IHeaderDictionary ToHeaderDictionary(this Dictionary<string, string> headers, params (string, string)[] values)
    {
        var result = new HeaderDictionary();
        foreach (var header in headers)
        {
            result.Add(header.Key, header.Value);
        }

        foreach (var header in values)
        {
            result.Add(header.Item1, header.Item2);
        }

        return result;
    }
}

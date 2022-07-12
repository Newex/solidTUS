using System;
using Microsoft.AspNetCore.Http;
using SolidTUS.Models;

namespace SolidTUS.Extensions;

/// <summary>
/// Generic http response helper methods
/// </summary>
public static class ResponseExtensions
{
    /// <summary>
    /// Add all the headers in the TUS response
    /// </summary>
    /// <param name="response">The http response</param>
    /// <param name="tusHttpResponse">The TUS response with headers</param>
    public static void AddTusHeaders(this HttpResponse response, TusHttpResponse tusHttpResponse)
    {
        foreach (var head in tusHttpResponse.Headers)
        {
            response.Headers.Add(head.Key, head.Value);
        }
    }
}

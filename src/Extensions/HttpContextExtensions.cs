using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using SolidTUS.Builders;
using SolidTUS.Contexts;
using SolidTUS.Handlers;
using SolidTUS.Models;
using SolidTUS.ProtocolFlows;

namespace SolidTUS.Extensions;

/// <summary>
/// <see cref="HttpContext"/> extension methods for TUS protocol
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Create new tus creation context.
    /// </summary>
    /// <param name="context">The http context</param>
    /// <param name="fileId">The file id</param>
    /// <returns>A tus creation context</returns>
    /// <exception cref="InvalidOperationException">Thrown if missing tus creation context service</exception>
    public static TusCreationContextBuilder TusCreation(this HttpContext context, string fileId)
    {
        return new(fileId);
    }

    /// <summary>
    /// Get tus metadata if there are any
    /// </summary>
    /// <param name="context">The http context</param>
    /// <returns>A tus metadata dictionary</returns>
    public static IReadOnlyDictionary<string, string>? TusMetadata(this HttpContext context)
    {
        if (context.Items[TusResult.Name] is not TusResult request)
        {
            throw new InvalidOperationException("Can only use this method in conjuction with either endpoint filter or action filter.");
        }

        return request.Metadata;
    }

    /// <summary>
    /// Create new tus upload context builder
    /// </summary>
    /// <param name="context">The http context</param>
    /// <param name="fileId">The file id</param>
    /// <returns>A tus upload context builder</returns>
    public static TusUploadContextBuilder TusUpload(this HttpContext context, string fileId)
    {
        return new(fileId);
    }

    internal static void SetErrorHeaders(this HttpContext context, HttpError error)
    {
        foreach (var (key, value) in error.Headers)
        {
            context.Response.Headers.Append(key, value);
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using SolidTUS.Builders;
using SolidTUS.Models;

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
    /// The tus file info
    /// </summary>
    /// <param name="context">The http context</param>
    /// <returns>A tus info</returns>
    /// <exception cref="InvalidOperationException">Thrown if missing tus creation context service</exception>
    public static TusInfo TusInfo(this HttpContext context)
    {
        return context.Items[TusResult.Name] is not TusResult request
            ? throw new InvalidOperationException("Can only use this method in conjuction with either endpoint filter or action filter.")
            : new TusInfo(
            request.Metadata,
            request.RawMetadata,
            request.FileSize,
            request.ChecksumContext
        );
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

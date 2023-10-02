using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SolidTUS.Constants;
using SolidTUS.Extensions;
using SolidTUS.Handlers;
using SolidTUS.Models;
using SolidTUS.Options;

namespace SolidTUS.ProtocolHandlers.ProtocolExtensions;

/// <summary>
/// Concatenation request handler
/// </summary>
public class ConcatenationRequestHandler
{
    private readonly IUploadMetaHandler uploadMetaHandler;
    private readonly long? maxSize;

    /// <summary>
    /// Instantiate a new object of <see cref="ConcatenationRequestHandler"/>
    /// </summary>
    /// <param name="uploadMetaHandler">The upload metadata handler</param>
    /// <param name="options">The TUS options</param>
    public ConcatenationRequestHandler(
        IUploadMetaHandler uploadMetaHandler,
        IOptions<TusOptions> options
    )
    {
        this.uploadMetaHandler = uploadMetaHandler;
        maxSize = options.Value.MaxSize;
    }

    /// <summary>
    /// Set the <see cref="UploadFileInfo.IsPartial"/> if present
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>A request context</returns>
    public static Result<RequestContext> SetIfUploadIsPartial(RequestContext context)
    {
        var concat = context.RequestHeaders[TusHeaderNames.UploadConcat].ToString();
        context.UploadFileInfo.IsPartial = !string.IsNullOrEmpty(concat);

        if (context.UploadFileInfo.IsPartial)
        {
            var isFinal = concat.StartsWith(TusHeaderValues.UploadFinal, StringComparison.OrdinalIgnoreCase);
            if (isFinal)
            {
                context.PartialMode = PartialMode.Final;
                return context.Wrap();
            }

            var isPartial = string.Equals(concat, TusHeaderValues.UploadPartial, StringComparison.OrdinalIgnoreCase);
            if (isPartial)
            {
                context.PartialMode = PartialMode.Partial;
                return context.Wrap();
            }

            if (!isFinal && !isPartial)
            {
                return HttpError.BadRequest("Upload-Concat must either be partial or final").Wrap();
            }

            return context.Wrap();
        }

        context.PartialMode = PartialMode.None;
        return context.Wrap();
    }

    /// <summary>
    /// Set the partial urls if request is final
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>A request context or an error</returns>
    public static Result<RequestContext> SetPartialUrlsIfFinal(RequestContext context)
    {
        if (context.PartialMode != PartialMode.Final)
        {
            return context.Wrap();
        }

        var header = context.RequestHeaders[TusHeaderNames.UploadConcat].ToString();
        var list = header[(header.IndexOf(";") + 1)..];
        if (list.Length == 0)
        {
            return HttpError.BadRequest("Must provide a list of files to concatenate").Wrap();
        }

        var urls = list.Split(" ");
        context.PartialUrls = urls;
        return context.Wrap();
    }

    /// <summary>
    /// Set the header for the <c>Upload-Concat</c> to the stored value from the metadata info file.
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>A request context</returns>
    public static RequestContext SetUploadConcatFinalUrls(RequestContext context)
    {
        if (context.PartialMode != PartialMode.Final)
        {
            return context;
        }

        var final = context.UploadFileInfo.ConcatHeaderFinal;
        context.ResponseHeaders.Add(TusHeaderNames.UploadConcat, final);
        return context;
    }

    /// <summary>
    /// Parse an input according to a template and returns the value of the token
    /// </summary>
    /// <param name="input">The input</param>
    /// <param name="template">The route template</param>
    /// <param name="token">The token value to extract from input</param>
    /// <returns>A string value of the token or null</returns>
    public static string? GetTemplateValue(string input, string template, string token)
    {
        string pattern = Regex.Escape(template).Replace("\\{", "{").Replace("\\}", "}");
        // pattern = Regex.Replace(pattern, @"{([^}]+)}", @"(?<$1>[^/]+)", RegexOptions.None);
        pattern = Regex.Replace(pattern, @"{([^:]+)(:[^}]+)?}", @"(?<$1>[^/]+)");

        var match = Regex.Match(input, pattern);
        if (match.Success && match.Groups[token].Success)
        {
            return match.Groups[token].Value;
        }

        return null;
    }
}


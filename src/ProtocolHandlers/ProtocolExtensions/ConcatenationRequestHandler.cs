using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using SolidTUS.Constants;
using SolidTUS.Contexts;
using SolidTUS.Extensions;
using SolidTUS.Handlers;
using SolidTUS.Models;
using SolidTUS.Options;

namespace SolidTUS.ProtocolHandlers.ProtocolExtensions;

/// <summary>
/// Concatenation request handler
/// </summary>
internal class ConcatenationRequestHandler
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
    /// Set the <see cref="TusResult.PartialMode"/>
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>A request context</returns>
    public static Result<TusResult> SetPartialMode(TusResult context)
    {
        var concat = context.RequestHeaders[TusHeaderNames.UploadConcat].ToString();

        if (!string.IsNullOrEmpty(concat))
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
    /// Check the partial urls if request is final and set them in the <see cref="TusResult"/>
    /// </summary>
    /// <param name="context">The request context</param>
    /// <returns>A request context or an error</returns>
    public static Result<TusResult> SetPartialFinalUrls(TusResult context)
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
        context.Urls = urls;
        return context.Wrap();
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


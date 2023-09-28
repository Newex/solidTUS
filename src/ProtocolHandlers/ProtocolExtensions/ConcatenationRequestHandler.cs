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
            }

            var isPartial = string.Equals(concat, TusHeaderValues.UploadPartial, StringComparison.OrdinalIgnoreCase);
            if (isPartial)
            {
                context.PartialMode = PartialMode.Partial;
            }

            if (!isFinal && !isPartial)
            {
                return HttpError.BadRequest("Upload-Concat must either be partial or final").Wrap();
            }
        }


        return context.Wrap();
    }

    /// <summary>
    /// Check if a final partial upload is valid
    /// </summary>
    /// <param name="context">The request context</param>
    /// <param name="template">The route template</param>
    /// <param name="partialParameterName">The partial id parameter name</param>
    /// <returns>A request context or an error</returns>
    public async Task<Result<RequestContext>> CheckIfUploadPartialIsFinalAsync(RequestContext context, string template, string partialParameterName)
    {
        var concat = context.RequestHeaders[TusHeaderNames.UploadConcat].ToString();
        if (string.IsNullOrWhiteSpace(concat))
        {
            return context.Wrap();
        }

        var isFinal = concat.StartsWith(TusHeaderValues.UploadFinal, StringComparison.OrdinalIgnoreCase);
        var uploadInfos = new List<UploadFileInfo>();
        HttpError? error = null;
        if (isFinal)
        {
            // final;
            var list = concat[(concat.IndexOf(';') + 1)..];
            if (list.Length == 0)
            {
                return HttpError.BadRequest("Must provide a list of files to concatenate").Wrap();
            }

            var partialIds = new string[list.Length];
            var pathCount = 0;
            var start = 0;
            for (var i = 0; i < list.Length; i++)
            {
                if (i == list.Length - 1)
                {
                    var path = list[start..];
                    if (path is null)
                    {
                        break;
                    }

                    var value = GetTemplateValue(path.ToString(), template, partialParameterName);
                    if (value is not null)
                    {
                        partialIds[pathCount] = value;
                    }
                }

                if (list[i] == ' ')
                {
                    var path = list[start..i];
                    if (path is null)
                    {
                        continue;
                    }

                    var value = GetTemplateValue(path.ToString(), template, partialParameterName);
                    if (value is not null)
                    {
                        partialIds[pathCount] = value;
                        pathCount++;
                        start = i + 1;
                    }
                }
            }

            if (partialIds.Length == 0)
            {
                error = HttpError.BadRequest("Missing partial uploads for final concatenation");
            }
            else
            {
                var partialTotalSize = 0L;
                foreach (var partialId in partialIds)
                {
                    if (partialId is null)
                    {
                        break;
                    }

                    var info = await uploadMetaHandler.GetResourceAsync(partialId, context.CancellationToken);
                    if (info is not null)
                    {
                        if (uploadInfos.Exists(x => x.FileId == partialId))
                        {
                            error = HttpError.BadRequest("Can only use partial upload once per concatenation");
                            break;
                        }

                        if (!info.Done)
                        {
                            error = HttpError.Forbidden("Can only merge partial uploads that are fully uploaded");
                            break;
                        }

                        uploadInfos.Add(info);
                        partialTotalSize += info.ByteOffset;

                        if (maxSize.HasValue && partialTotalSize > maxSize.Value)
                        {
                            error = HttpError.EntityTooLarge("Partial files exceed Tus-Max-Size limit");
                            break;
                        }
                    }
                    else
                    {
                        error = HttpError.NotFound($"Could not find file: {partialId} used in concatenation");
                        break;
                    }
                }

                context.PartialFinalUploadInfos = uploadInfos;
            }
        }

        if (error.HasValue)
        {
            return error.Value.Wrap();
        }

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


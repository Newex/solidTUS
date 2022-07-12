using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Options;
using SolidTUS.Constants;
using SolidTUS.Models;
using SolidTUS.Options;
using SolidTUS.Validators;

namespace SolidTUS.ProtocolHandlers;

/// <summary>
/// Options request handler
/// </summary>
public class OptionsRequestHandler
{
    private readonly long? maxSize;
    private readonly IEnumerable<IChecksumValidator> validators;

    /// <summary>
    /// Instantiate a new object of <see cref="OptionsRequestHandler"/>
    /// </summary>
    /// <param name="options">The TUS options</param>
    /// <param name="validators"></param>
    public OptionsRequestHandler(IOptions<TusOptions> options, IEnumerable<IChecksumValidator> validators)
    {
        maxSize = options.Value.MaxSize;
        this.validators = validators;
    }

    /// <summary>
    /// Constructs a discovery response that indicates the servers capabilities
    /// </summary>
    /// <returns>A TUS response</returns>
    public TusHttpResponse ServerFeatureAnnouncements()
    {
        var response = new TusHttpResponse();
        response.Headers.Add(TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion);
        response.Headers.Add(TusHeaderNames.Version, TusHeaderValues.TusServerVersions);

        if (maxSize.HasValue)
        {
            response.Headers.Add(TusHeaderNames.MaxSize, maxSize.Value.ToString());
        }

        // TUS protocol extensions
        var checksumAlgorithms = string.Join(",", validators.Select(v => v.AlgorithmName));
        response.Headers.Add(TusHeaderNames.ChecksumAlgorithm, checksumAlgorithms);
        response.Headers.Add(TusHeaderNames.Extension, TusHeaderValues.TusSupportedExtensions);
        return response;
    }
}

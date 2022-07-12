using Microsoft.Extensions.DependencyInjection;

namespace SolidTUS.Extensions;

/// <summary>
/// Extension helpers for registering dependencies
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add services for TUS
    /// </summary>
    /// <remarks>
    /// If using the default storage handler, remember to set the upload directory
    /// </remarks>
    /// <param name="services">The services collection</param>
    /// <returns>A TUS builder</returns>
    public static TusBuilder AddTus(this IServiceCollection services)
    {
        return TusBuilder.Create(services);
    }
}

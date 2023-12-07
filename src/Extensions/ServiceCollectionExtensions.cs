using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolidTUS.Builders;
using SolidTUS.Options;

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

    /// <summary>
    /// Add services for TUS and load <see cref="TusOptions"/> and <see cref="FileStorageOptions"/> from configuration
    /// </summary>
    /// <param name="services">The services collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>A TUS builder</returns>
    [UnconditionalSuppressMessage("Trimming",
    "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
    Justification = "Options annotated with dynamic members attribute")]
    [UnconditionalSuppressMessage("AOT",
    "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
    Justification = "Options annotated with dynamic members attribute")]
    public static TusBuilder AddTus(this IServiceCollection services, IConfiguration configuration)
    {
        var tusSection = configuration.GetSection(TusOptions.TusConfigurationSection);
        services.Configure<TusOptions>(tusSection);
        services.Configure<FileStorageOptions>(tusSection);
        return TusBuilder.Create(services);
    }
}

using System;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SolidTUS.Contexts;
using SolidTUS.Handlers;
using SolidTUS.Models;
using SolidTUS.Options;
using SolidTUS.ProtocolFlows;
using SolidTUS.ProtocolHandlers;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;
using SolidTUS.Validators;

namespace SolidTUS.Extensions;

/// <summary>
/// Tus service builder pattern
/// </summary>
public sealed class TusBuilder
{
    private readonly ServiceDescriptor uploadMetaDescriptor = new(typeof(IUploadMetaHandler), typeof(FileUploadMetaHandler), ServiceLifetime.Scoped);
    private readonly ServiceDescriptor uploadStorageDescriptor = new(typeof(IUploadStorageHandler), typeof(FileUploadStorageHandler), ServiceLifetime.Scoped);

    private readonly IServiceCollection services;

    private TusBuilder(IServiceCollection services)
    {
        this.services = services;
    }

    /// <summary>
    /// Add an upload storage handler implementation
    /// </summary>
    /// <typeparam name="T">The specific upload storage handler</typeparam>
    /// <returns>builder</returns>
    public TusBuilder AddStorageHandler<T>()
        where T : class, IUploadStorageHandler
    {
        services.Remove(uploadStorageDescriptor);
        services.TryAddScoped<IUploadStorageHandler, T>();
        return this;
    }

    /// <summary>
    /// Add an upload meta handler
    /// </summary>
    /// <typeparam name="T">The specific upload meta handler</typeparam>
    /// <returns>builder</returns>
    public TusBuilder AddMetaHandler<T>()
        where T : class, IUploadMetaHandler
    {
        services.Remove(uploadMetaDescriptor);
        services.TryAddScoped<IUploadMetaHandler, T>();
        return this;
    }

    /// <summary>
    /// Add a checksum validator
    /// </summary>
    /// <typeparam name="T">The specific implementation type</typeparam>
    /// <returns>builder</returns>
    public TusBuilder AddChecksumValidator<T>()
        where T : class, IChecksumValidator
    {
        services.AddScoped<IChecksumValidator, T>();
        return this;
    }

    /// <summary>
    /// Configures TUS options
    /// </summary>
    /// <param name="options">The TUS options</param>
    /// <returns>builder</returns>
    public TusBuilder Configuration(Action<TusOptions> options)
    {
        services.Configure(options);
        return this;
    }

    /// <summary>
    /// Configure the default file storage upload handler
    /// </summary>
    /// <param name="options">File storage options</param>
    /// <returns>builder</returns>
    public TusBuilder FileStorageConfiguration(Action<FileStorageOptions> options)
    {
        services.Configure(options);
        return this;
    }

    internal static TusBuilder Create(IServiceCollection services)
    {
        var builder = new TusBuilder(services);

        builder.services.TryAddSingleton<ISystemClock>();

        builder.services.TryAddScoped<CommonRequestHandler>();
        builder.services.TryAddScoped<PatchRequestHandler>();
        builder.services.TryAddScoped<PostRequestHandler>();
        builder.services.TryAddScoped<OptionsRequestHandler>();
        builder.services.TryAddScoped<ExpirationRequestHandler>();

        builder.services.TryAddScoped<UploadFlow>();
        builder.services.TryAddScoped<CreationFlow>();
        builder.services.TryAddScoped<ChecksumRequestHandler>();
        builder.services.Add(builder.uploadMetaDescriptor);
        builder.services.Add(builder.uploadStorageDescriptor);
        builder.services.Configure<TusOptions>(_ => { });
        builder.services.Configure<MvcOptions>(options =>
        {
            options.ModelMetadataDetailsProviders.Add(
                new ExcludeBindingMetadataProvider(typeof(TusUploadContext))
            );
            options.ModelMetadataDetailsProviders.Add(
                new ExcludeBindingMetadataProvider(typeof(TusCreationContext))
            );
        });

        builder.services.AddScoped<IChecksumValidator, SHA1ChecksumValidator>();
        builder.services.AddScoped<IChecksumValidator, MD5ChecksumValidator>();
        return builder;
    }
}

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Internal;
using SolidTUS.Contexts;
using SolidTUS.Handlers;
using SolidTUS.Jobs;
using SolidTUS.Options;
using SolidTUS.ProtocolFlows;
using SolidTUS.ProtocolHandlers;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;
using SolidTUS.Validators;
using SolidTUS.Wrappers;

namespace SolidTUS.Extensions;

/// <summary>
/// Tus service builder pattern
/// </summary>
public sealed class TusBuilder
{
    private readonly ServiceDescriptor uploadMetaDescriptor = new(typeof(IUploadMetaHandler), typeof(FileUploadMetaHandler), ServiceLifetime.Singleton);
    private readonly ServiceDescriptor uploadStorageDescriptor = new(typeof(IUploadStorageHandler), typeof(FileUploadStorageHandler), ServiceLifetime.Singleton);
    private readonly ServiceDescriptor expiredHandleDescriptor = new(typeof(IExpiredUploadHandler), typeof(FileExpiredUploadHandler), ServiceLifetime.Singleton);

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
        services.TryAddSingleton<IUploadStorageHandler, T>();
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
        services.TryAddSingleton<IUploadMetaHandler, T>();
        return this;
    }

    /// <summary>
    /// Add expired upload handler
    /// </summary>
    /// <typeparam name="T">The specific expired handler</typeparam>
    /// <returns>builder</returns>
    public TusBuilder AddExpirationHandler<T>()
        where T : class, IExpiredUploadHandler
    {
        services.Remove(expiredHandleDescriptor);
        services.TryAddSingleton<IExpiredUploadHandler, T>();
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

    /// <summary>
    /// Add background service job runner that scans periodically for expired uploads.
    /// </summary>
    /// <remarks>
    /// Every <see cref="TusOptions.ExpirationJobRunnerInterval"/> the <see cref="IExpiredUploadHandler.StartScanForExpiredUploadsAsync"/> will run
    /// </remarks>
    /// <returns>builder</returns>
    public TusBuilder WithExpirationJobRunner()
    {
        services.AddHostedService<ExpirationJob>();
        return this;
    }

    internal static TusBuilder Create(IServiceCollection services)
    {
        var builder = new TusBuilder(services);

        builder.services.TryAddSingleton<ISystemClock, SystemClock>();

        builder.services.TryAddScoped<CommonRequestHandler>();
        builder.services.TryAddScoped<PatchRequestHandler>();
        builder.services.TryAddScoped<PostRequestHandler>();
        builder.services.TryAddScoped<OptionsRequestHandler>();
        builder.services.TryAddScoped<TerminationRequestHandler>();
        builder.services.TryAddScoped<ExpirationRequestHandler>();
        builder.services.TryAddScoped<ChecksumRequestHandler>();

        builder.services.TryAddScoped<UploadFlow>();
        builder.services.TryAddScoped<CreationFlow>();
        builder.services.Add(builder.uploadMetaDescriptor);
        builder.services.Add(builder.uploadStorageDescriptor);
        builder.services.Add(builder.expiredHandleDescriptor);
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

        builder.services.TryAddScoped<ILinkGeneratorWrapper, LinkGeneratorWrapper>();
        return builder;
    }
}

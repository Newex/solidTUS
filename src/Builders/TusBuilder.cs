using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

namespace SolidTUS.Builders;

/// <summary>
/// Tus service builder pattern
/// </summary>
public sealed class TusBuilder
{
    private readonly ServiceDescriptor uploadMetaDescriptor = new(typeof(IUploadMetaHandler), typeof(FileUploadMetaHandler), ServiceLifetime.Singleton);
    private readonly ServiceDescriptor uploadStorageDescriptor = new(typeof(IUploadStorageHandler), typeof(FileUploadStorageHandler), ServiceLifetime.Singleton);
    private readonly ServiceDescriptor expiredHandleDescriptor = new(typeof(IExpiredUploadHandler), typeof(FileExpiredUploadHandler), ServiceLifetime.Singleton);
    private readonly ServiceDescriptor metadataValidatorDescriptor = new(typeof(MetadataValidatorFunc), typeof(MetadataValidatorFunc), ServiceLifetime.Singleton);
    private readonly ServiceDescriptor allowEmptyMetadataDescriptor = new(typeof(AllowEmptyMetadataFunc), typeof(AllowEmptyMetadataFunc), ServiceLifetime.Singleton);

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
    public TusBuilder AddStorageHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
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
    public TusBuilder AddMetaHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
        where T : class, IUploadMetaHandler
    {
        services.Remove(uploadMetaDescriptor);
        services.TryAddSingleton<IUploadMetaHandler, T>();
        return this;
    }

    /// <summary>
    /// Set a custom metadata validator.
    /// </summary>
    /// <remarks>
    /// Default allows any entry.
    /// </remarks>
    /// <param name="validator">Validate the parsed <c>Upload-Metadata</c> header</param>
    /// <returns>builder</returns>
    public TusBuilder SetMetadataValidator(Func<Dictionary<string, string>, bool> validator)
    {
        services.Remove(metadataValidatorDescriptor);
        services.TryAddSingleton<MetadataValidatorFunc>(provider => (m) => validator(m));
        return this;
    }

    /// <summary>
    /// Whether to allow empty metadata entries.
    /// </summary>
    /// <remarks>Default allows empty entries.</remarks>
    /// <param name="allow">True if it should allow empty metadata entries otherwise false</param>
    /// <returns>builder</returns>
    public TusBuilder AllowEmptyMetadata(bool allow)
    {
        services.Remove(allowEmptyMetadataDescriptor);
        services.TryAddSingleton<AllowEmptyMetadataFunc>(provider => () => allow);
        return this;
    }

    /// <summary>
    /// Add expired upload handler
    /// </summary>
    /// <typeparam name="T">The specific expired handler</typeparam>
    /// <returns>builder</returns>
    public TusBuilder AddExpirationHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
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
    public TusBuilder AddChecksumValidator<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
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

        builder.services.TryAddSingleton<MetadataValidatorFunc>(provider => MetadataValidator.Validator);
        builder.services.TryAddSingleton<AllowEmptyMetadataFunc>(provider => MetadataValidator.AllowEmptyMetadata);
        builder.services.AddScoped<IChecksumValidator, SHA1ChecksumValidator>();
        builder.services.AddScoped<IChecksumValidator, MD5ChecksumValidator>();

        builder.services.TryAddScoped<ResourceCreationHandler>();
        builder.services.TryAddScoped<UploadHandler>();
        builder.services.TryAddScoped<ILinkGeneratorWrapper, LinkGeneratorWrapper>();
        return builder;
    }
}

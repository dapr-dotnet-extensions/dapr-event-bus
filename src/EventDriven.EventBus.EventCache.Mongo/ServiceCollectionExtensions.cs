using EventDriven.DependencyInjection.URF.Mongo;
using EventDriven.EventBus.Abstractions;
using EventDriven.EventBus.EventCache.Mongo;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using URF.Core.Abstractions;
using URF.Core.Mongo;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for <see cref="T:IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add Mongo event cache to the provided <see cref="T:IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:IServiceCollection" /></param>
    /// <param name="configuration">The application's <see cref="IConfiguration"/>.</param>
    /// <returns>The original <see cref="T:IServiceCollection" />.</returns>
    public static IServiceCollection AddMongoEventCache(this IServiceCollection services,
        IConfiguration configuration)
    {
        var eventCacheOptions = new MongoEventCacheOptions
        {
            EnableEventCacheCleanup = true,
        };
        var mongoOptionsConfigSection = configuration.GetSection(nameof(MongoEventCacheOptions));
        mongoOptionsConfigSection.Bind(eventCacheOptions);
        if (!mongoOptionsConfigSection.Exists())
            throw new Exception($"Configuration section '{nameof(MongoEventCacheOptions)}' not present in app settings.");
        if (eventCacheOptions.EnableEventCache && string.IsNullOrWhiteSpace(eventCacheOptions.AppName))
            throw new Exception($"Configuration section 'MongoEventCacheOptions:AppName' must be specified.");

        services.Configure<MongoEventCacheOptions>(options =>
        {
            options.AppName = eventCacheOptions.AppName;
            options.EnableEventCache = eventCacheOptions.EnableEventCache;
            options.EventCacheTimeout = eventCacheOptions.EventCacheTimeout;
            options.EnableEventCacheCleanup = eventCacheOptions.EnableEventCacheCleanup;
            options.EventCacheCleanupInterval = eventCacheOptions.EventCacheCleanupInterval;
        });
        services.AddSingleton<EventCacheOptions>(eventCacheOptions);

        if (!eventCacheOptions.EnableEventCache) return services;
        services.AddSingleton<IEventCache, MongoEventCache>();
        services.AddSingleton<IEventHandlingRepository<IntegrationEvent>,
            MongoEventHandlingRepository<IntegrationEvent>>();
        services.AddSingleton<IDocumentRepository<EventWrapperDto>, DocumentRepository<EventWrapperDto>>();
        return services.AddMongoDbSettings<MongoStoreDatabaseSettings, EventWrapperDto>(configuration);
    }

    /// <summary>
    /// Add Mongo event cache to the provided <see cref="T:IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:IServiceCollection" /></param>
    /// <param name="configureMongoStoreOptions">Configure Mongo store options.</param>
    /// <param name="configureEventCacheOptions">Configure event cache options.</param>
    /// <param name="lifetime">Service lifetime.</param>
    /// <returns>The original <see cref="T:IServiceCollection" />.</returns>
    public static IServiceCollection AddMongoEventCache(this IServiceCollection services,
        Action<MongoStoreDatabaseSettings>? configureMongoStoreOptions = null,
        Action<MongoEventCacheOptions>? configureEventCacheOptions = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        var eventCacheOptions = new MongoEventCacheOptions
        {
            EnableEventCache = true,
            EnableEventCacheCleanup = true
        };

        configureEventCacheOptions?.Invoke(eventCacheOptions);
        services.Configure<MongoEventCacheOptions>(options =>
        {
            options.AppName = eventCacheOptions.AppName;
            options.EnableEventCache = eventCacheOptions.EnableEventCache;
            options.EventCacheTimeout = eventCacheOptions.EventCacheTimeout;
            options.EnableEventCacheCleanup = eventCacheOptions.EnableEventCacheCleanup;
            options.EventCacheCleanupInterval = eventCacheOptions.EventCacheCleanupInterval;
        });

        switch (lifetime)
        {
            case ServiceLifetime.Transient:
                services.AddTransient<EventCacheOptions>();
                if (!eventCacheOptions.EnableEventCache) return services;
                services.AddTransient<IEventCache, MongoEventCache>();
                services.AddTransient<IEventHandlingRepository<IntegrationEvent>,
                    MongoEventHandlingRepository<IntegrationEvent>>();
                services.AddTransient<IDocumentRepository<EventWrapperDto>, DocumentRepository<EventWrapperDto>>();
                break;
            case ServiceLifetime.Scoped:
                services.AddScoped<EventCacheOptions>();
                if (!eventCacheOptions.EnableEventCache) return services;
                services.AddScoped<IEventCache, MongoEventCache>();
                services.AddScoped<IEventHandlingRepository<IntegrationEvent>,
                    MongoEventHandlingRepository<IntegrationEvent>>();
                services.AddScoped<IDocumentRepository<EventWrapperDto>, DocumentRepository<EventWrapperDto>>();
                break;
            default:
                services.AddSingleton<EventCacheOptions>(eventCacheOptions);
                if (!eventCacheOptions.EnableEventCache) return services;
                services.AddSingleton<IEventCache, MongoEventCache>();
                services.AddSingleton<IEventHandlingRepository<IntegrationEvent>,
                    MongoEventHandlingRepository<IntegrationEvent>>();
                services.AddSingleton<IDocumentRepository<EventWrapperDto>, DocumentRepository<EventWrapperDto>>();
                break;
        }
        
        return services.AddMongoStoreDatabaseSettings(configureMongoStoreOptions, lifetime);
    }

    private static IServiceCollection AddMongoStoreDatabaseSettings(this IServiceCollection services,
        Action<MongoStoreDatabaseSettings>? configureMongoStoreOptions = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        var databaseSettings = new MongoStoreDatabaseSettings();
        if (configureMongoStoreOptions != null)
        {
            configureMongoStoreOptions(databaseSettings);
            services.Configure(configureMongoStoreOptions);
        }

        switch (lifetime)
        {
            case ServiceLifetime.Transient:
                return services.AddTransient(_ =>
                {
                    var client = new MongoClient(databaseSettings.ConnectionString);
                    var database = client.GetDatabase(databaseSettings.DatabaseName);
                    return database.GetCollection<EventWrapperDto>(databaseSettings.CollectionName);
                });
            case ServiceLifetime.Scoped:
                return services.AddScoped(_ =>
                {
                    var client = new MongoClient(databaseSettings.ConnectionString);
                    var database = client.GetDatabase(databaseSettings.DatabaseName);
                    return database.GetCollection<EventWrapperDto>(databaseSettings.CollectionName);
                });
            default:
                return services.AddSingleton(_ =>
                {
                    var client = new MongoClient(databaseSettings.ConnectionString);
                    var database = client.GetDatabase(databaseSettings.DatabaseName);
                    return database.GetCollection<EventWrapperDto>(databaseSettings.CollectionName);
                });
        }
    }
}
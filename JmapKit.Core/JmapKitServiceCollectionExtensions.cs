using Microsoft.Extensions.DependencyInjection;

namespace JmapKit;

/// <summary>
/// Extension methods for registering JmapKit with an <see cref="IServiceCollection"/>.
/// </summary>
public static class JmapKitServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IJmapClient"/> and it's default implementation <see cref="JmapClient"/> with the
    /// service collection.
    ///
    /// Registers a <see cref="JmapTokenCredential"/> by default if one is not yet registered.
    ///
    /// Registers a <see cref="JmapClientOptions"/> by default if one is not yet registered.
    /// 
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddJmapClient(this IServiceCollection services)
    {
        // Register a default if one was not registered by user.
        if (!services.Contains(ServiceDescriptor.Singleton(typeof(JmapTokenCredential))))
            services.AddSingleton<JmapTokenCredential>();
        
        // Register a default options object if one was not registered by user.
        if (!services.Contains(ServiceDescriptor.Singleton(typeof(JmapClientOptions))))
            services.AddSingleton<JmapClientOptions>();
        
        services.AddHttpClient<IJmapClient, JmapClient>()
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
            });

        return services;
    }
}
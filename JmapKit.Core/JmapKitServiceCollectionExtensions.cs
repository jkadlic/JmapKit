using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
    /// Registers a <see cref="JmapSerializerOptions"/> by default if one is not yet registered.
    ///
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureJson">
    /// Optionally adjusts the JSON configuration used by the JmapClient. Ignored if a
    /// <see cref="JmapSerializerOptions"/> is already registered.
    /// </param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddJmapClient(
        this IServiceCollection services,
        Action<JsonSerializerOptions>? configureJson = null)
    {
        services.TryAddSingleton<JmapTokenCredential>();
        services.TryAddSingleton<JmapClientOptions>();
        services.TryAddSingleton(new JmapSerializerOptions(configureJson));

        services.AddHttpClient<IJmapClient, JmapClient>()
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
            });

        return services;
    }
}

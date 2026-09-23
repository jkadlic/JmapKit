using System.Text.Json;

namespace JmapKit;

/// <summary>
/// The JSON configuration used for all JMAP traffic, built from the library's defaults and then handed to the caller for adjustment.
/// </summary>
public sealed class JmapSerializerOptions
{
    /// <summary>
    /// Internal types that shape the JMAP envelope. These must not be overridden by user-supplied converters.
    /// </summary>
    private static readonly Type[] ProtocolTypes = [typeof(JmapMethodInvocation), typeof(JmapMethodResponse)];

    /// <summary>
    /// Creates the options using only the library's defaults.
    /// </summary>
    public JmapSerializerOptions() : this(null) { }

    /// <summary>
    /// Creates the options from the library's defaults, then applies <paramref name="configure"/>.
    /// </summary>
    /// <param name="configure">
    /// Adjusts the library's options. Add converters for your own types here; the JMAP property names
    /// themselves are declared on each type and are not affected by a naming policy.
    /// </param>
    /// <exception cref="JmapConfigurationException">
    /// <paramref name="configure"/> registered a converter for a type whose converter defines the JMAP
    /// protocol envelope.
    /// </exception>
    public JmapSerializerOptions(Action<JsonSerializerOptions>? configure)
    {
        var options = JmapJson.Create();
        configure?.Invoke(options);

        // User-supplied converter might unintentionally override a type's [JsonConverter] definition. In the case
        // of JmapMethodInvocation or JmapMethodResponse, that would be a fatal and subtle failure.
        foreach (var type in ProtocolTypes)
        {
            var hijacked = options.Converters.FirstOrDefault(c => c.CanConvert(type));
            if (hijacked is not null)
            {
                throw new JmapConfigurationException(
                    $"'{hijacked.GetType().Name}' claims '{type.Name}'. User-supplied converters must not "
                    + "claim protected JMAP envelope types 'JmapMethodInvocation' and 'JmapMethodResponse'.");
            }
        }

        Options = options;
    }

    /// <summary>
    /// The configured options. Do not mutate after the client has been used;
    /// <see cref="JsonSerializerOptions"/> becomes read-only on first use.
    /// </summary>
    public JsonSerializerOptions Options { get; }
}
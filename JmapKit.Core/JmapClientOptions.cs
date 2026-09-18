namespace JmapKit;

/// <summary>
/// Configuration for <see cref="JmapClient"/>.
/// </summary>
public class JmapClientOptions
{
    /// <summary>
    /// The host to resolve the JMAP session from (no scheme), e.g. <c>jmap.fastmail.com</c>.
    /// </summary>
    public string Host { get; }

    /// <summary>
    /// Creates options using the <c>JMAP_HOST</c> environment variable.
    /// </summary>
    /// <exception cref="JmapConfigurationException">The <c>JMAP_HOST</c> environment variable is not set.</exception>
    public JmapClientOptions()
    {
        var host = Environment.GetEnvironmentVariable("JMAP_HOST");
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JMAP_HOST")))
            throw new JmapConfigurationException("Environment variable 'JMAP_HOST' is required to configure JmapClientOptions.");

        Host = host!;
    }

    /// <summary>
    /// Creates options with an explicit host.
    /// </summary>
    /// <param name="host">The host to resolve the JMAP session from (no scheme).</param>
    public JmapClientOptions(string host)
    {
        Host = host;
    }
}
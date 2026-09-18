namespace JmapKit;

/// <summary>
/// Thrown when <see cref="JmapClientOptions"/> could not be configured.
/// </summary>
/// <param name="reason">Why the options could not be configured.</param>
public class JmapConfigurationException(string reason) : JmapException($"Failed to configure JmapClient. {reason}");
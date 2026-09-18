namespace JmapKit;

/// <summary>
/// Thrown when a <see cref="JmapTokenCredential"/> could not be configured.
/// </summary>
/// <param name="reason">Why the credential could not be configured.</param>
public class JmapCredentialException(string reason) : JmapException($"Failed to configure JmapCredential. {reason}.");
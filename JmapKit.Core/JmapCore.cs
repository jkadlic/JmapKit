namespace JmapKit;

/// <summary>
/// Represents the Core object in a JMAP server. This is a basic JMAP object only used for /echo purposes.
/// </summary>
public sealed class JmapCore : IJmapObject
{
    /// <inheritdoc />
    public static string JmapName { get; } = "Core";

    /// <inheritdoc />
    public static JmapCapability[] JmapCapabilities { get; } = [JmapCoreCapability.Core];

    /// <inheritdoc />
    public static JmapMethod[] SupportedMethods { get; } = [JmapMethod.Echo];
}

namespace JmapKit;

/// <summary>
/// Represents the Core object in a JMAP server. This is a basic JMAP object only used for /echo purposes.
/// </summary>
public sealed class JmapCore : IJmapObject
{
    /// <summary>
    /// Name of the JMAP object type.
    /// </summary>
    public static string JmapName { get; } = "Core";

    /// <summary>
    /// Array of capabilities required to make queries against this type of JMAP object.
    /// </summary>
    public static JmapCapability[] JmapCapabilities { get; } = [JmapCoreCapability.Core];
}

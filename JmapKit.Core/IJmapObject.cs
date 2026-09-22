namespace JmapKit;

/// <summary>
/// Marks a type as a JMAP data type usable with the generic "/get", "/set", "/changes",
/// "/copy", "/query", and "/queryChanges" method calls.
/// </summary>
public interface IJmapObject
{
    /// <summary>
    /// The data type's name.
    /// </summary>
    static abstract string JmapName { get; }

    /// <summary>
    /// The capability URIs required to use this data type. Sent verbatim as the request's "using" property.
    /// </summary>
    static abstract JmapCapability[] JmapCapabilities { get; }

    /// <summary>
    /// The methods that are supported for this data type.
    /// </summary>
    static abstract JmapMethod[] SupportedMethods { get; }
}

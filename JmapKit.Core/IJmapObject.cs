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
    /// The capability this data type belongs to.
    /// </summary>
    static abstract JmapCapability Capability { get; }
}
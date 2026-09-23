namespace JmapKit;

/// <summary>
/// Marks a type as a JMAP data type usable with the generic "/get", "/set", "/changes",
/// "/copy", "/query", and "/queryChanges" method calls.
/// </summary>
/// <remarks>
/// Implementations should carry
/// <see cref="System.Text.Json.Serialization.JsonPropertyNameAttribute"/> on every serialized property.
/// JMAP property names are fixed literals defined by the relevant RFC rather than a transform of a C#
/// identifier &#8212; <see href="https://www.rfc-editor.org/rfc/rfc8620#section-1.1">RFC 8620 §1.1</see>
/// states only that they are case-sensitive, so a name that does not match exactly is an unrecognised
/// property, not a differently cased one. Naming them explicitly also keeps the type correct when it is
/// serialized with options this library did not supply. Properties left unannotated fall back to the
/// camelCase policy this library applies to its own traffic, which is correct for most names but is an
/// inference rather than a guarantee.
/// </remarks>
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

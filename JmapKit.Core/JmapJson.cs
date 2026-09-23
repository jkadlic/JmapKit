using System.Text.Json;

namespace JmapKit;

/// <summary>
/// The serializer options used for all JMAP traffic.
/// </summary>
/// <remarks>
/// JMAP property names are fixed literals defined by the relevant RFC, not a transform of a C# identifier
/// (<see href="https://www.rfc-editor.org/rfc/rfc8620#section-1.1">RFC 8620 §1.1</see> only states that they
/// are case sensitive). Types owned by this library therefore pin their names with
/// <see cref="System.Text.Json.Serialization.JsonPropertyNameAttribute"/>, which takes precedence over any
/// naming policy. The camelCase policy here is the fallback for <see cref="IJmapObject"/> implementations
/// that have not done so.
/// </remarks>
internal static class JmapJson
{
    /// <summary>
    /// The shared options instance. <see cref="JsonSerializerOptions"/> caches type metadata per instance and
    /// becomes read-only on first use, so this is deliberately created once rather than per call.
    /// </summary>
    public static JsonSerializerOptions Default { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}

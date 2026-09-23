using System.Text.Json;

namespace JmapKit;

/// <summary>
/// The library's own serializer defaults, before any caller configuration is applied.
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
    /// A shared instance carrying the library defaults, used when no configured instance is reachable.
    /// </summary>
    public static JsonSerializerOptions Default { get; } = Create();

    /// <summary>
    /// Creates a fresh <see cref="JsonSerializerOptions"/> carrying the library defaults.
    /// </summary>
    public static JsonSerializerOptions Create() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}

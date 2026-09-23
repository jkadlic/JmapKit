using System.Text.Json.Serialization;

namespace JmapKit;

/// <summary>
/// A JMAP method-level error, returned in place of a method's normal result
/// (<see href="https://www.rfc-editor.org/rfc/rfc8620#section-3.6.1">RFC 8620 §3.6.1</see>).
/// </summary>
public sealed record JmapMethodError
{
    /// <summary>
    /// The error type, e.g. "unknownMethod" or "invalidArguments".
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// A human-readable description of the error, if the server provided one.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }
}
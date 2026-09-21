using System.Text.Json.Serialization;

namespace JmapKit;

/// <summary>
/// A JMAP response, returned from the API endpoint (<see href="https://www.rfc-editor.org/rfc/rfc8620#section-3.4">RFC 8620 §3.4</see>).
/// </summary>
public sealed class JmapResponse
{
    /// <summary>
    /// The responses to each method call, in the same order the calls were made.
    /// </summary>
    [JsonPropertyName("methodResponses")]
    public required JmapMethodResponse[] MethodResponses { get; init; }

    /// <summary>
    /// Client-provided ids from this request, mapped to the server-assigned ids they created.
    /// </summary>
    [JsonPropertyName("createdIds")]
    public Dictionary<string, JmapId>? CreatedIds { get; init; }

    /// <summary>
    /// The current state of the session; changes whenever anything in the session object changes.
    /// </summary>
    [JsonPropertyName("sessionState")]
    public required string SessionState { get; init; }
}
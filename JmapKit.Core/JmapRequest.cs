using System.Text.Json.Serialization;

namespace JmapKit;

/// <summary>
/// A JMAP request, sent to the API endpoint (<see href="https://www.rfc-editor.org/rfc/rfc8620#section-3.3">RFC 8620 §3.3</see>).
/// </summary>
public sealed class JmapRequest
{
    /// <summary>
    /// The capability URIs used by the method calls in this request.
    /// </summary>
    [JsonPropertyName("using")]
    public required JmapCapability[] Using { get; init; }

    /// <summary>
    /// The method calls to invoke, in order.
    /// </summary>
    [JsonPropertyName("methodCalls")]
    public required JmapMethodInvocation[] MethodCalls { get; init; }

    /// <summary>
    /// Client-provided ids from prior requests, mapped to the server-assigned ids they created.
    /// </summary>
    [JsonPropertyName("createdIds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JmapId>? CreatedIds { get; init; }
}
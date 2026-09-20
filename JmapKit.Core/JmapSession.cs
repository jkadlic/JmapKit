using System.Text.Json;
using System.Text.Json.Serialization;

namespace JmapKit;

/// <summary>
/// An account within a <see cref="JmapSession"/>, keyed by account id in <see cref="JmapSession.Accounts"/>.
/// </summary>
public sealed class JmapSessionAccount
{
    /// <summary>
    /// A user-friendly display name for the account.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Whether this account belongs to the authenticated user, rather than a shared account.
    /// </summary>
    [JsonPropertyName("isPersonal")]
    public required bool IsPersonal { get; init; }

    /// <summary>
    /// Whether the authenticated user has read-only access to this account.
    /// </summary>
    [JsonPropertyName("isReadOnly")]
    public required bool IsReadOnly { get; init; }

    /// <summary>
    /// Capability-specific properties for this account, keyed by capability URI.
    /// </summary>
    [JsonPropertyName("accountCapabilities")]
    public Dictionary<string, JsonElement> AccountCapabilities { get; init; } = [];
}

/// <summary>
/// A JMAP session, resolved via the well-known entrypoint
/// (<see href="https://www.rfc-editor.org/rfc/rfc8620#section-2">RFC 8620 §2</see>).
/// </summary>
public sealed class JmapSession
{
    /// <summary>
    /// A display name for the authenticated user.
    /// </summary>
    [JsonPropertyName("username")]
    public required string Username { get; init; }

    /// <summary>
    /// The URL to send API requests to.
    /// </summary>
    [JsonPropertyName("apiUrl")]
    public required string ApiUrl { get; init; }

    /// <summary>
    /// The URL for binary data upload, as a URI Template.
    /// </summary>
    [JsonPropertyName("uploadUrl")]
    public required string UploadUrl { get; init; }

    /// <summary>
    /// The URL for binary data download, as a URI Template.
    /// </summary>
    [JsonPropertyName("downloadUrl")]
    public required string DownloadUrl { get; init; }

    /// <summary>
    /// The URL to connect to for push events, as a URI Template.
    /// </summary>
    [JsonPropertyName("eventSourceUrl")]
    public required string EventSourceUrl { get; init; }

    /// <summary>
    /// A string identifying this session; changes whenever anything in the session object changes.
    /// </summary>
    [JsonPropertyName("state")]
    public required string State { get; init; }

    /// <summary>
    /// The accounts the authenticated user has access to, keyed by account id.
    /// </summary>
    [JsonPropertyName("accounts")]
    public required Dictionary<string, JmapSessionAccount> Accounts { get; init; }

    /// <summary>
    /// The default account id to use for each capability, keyed by capability URI.
    /// </summary>
    [JsonPropertyName("primaryAccounts")]
    public required Dictionary<string, string> PrimaryAccounts { get; init; }

    /// <summary>
    /// Server-wide capability properties, keyed by capability URI.
    /// </summary>
    [JsonPropertyName("capabilities")]
    public required Dictionary<string, JsonElement> Capabilities { get; init; }
}
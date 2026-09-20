using System.Net;
using System.Text;
using System.Text.Json;

namespace JmapKit;

internal static class JmapClientDefaults
{
    public const string ApiEntrypoint = ".well-known/jmap";
}

/// <summary>
/// Default <see cref="IJmapClient"/> implementation. Register via
/// <see cref="JmapKitServiceCollectionExtensions.AddJmapClient"/> rather than constructing directly.
/// </summary>
public sealed class JmapClient : IJmapClient
{
    private readonly HttpClient _http;
    private readonly JmapTokenCredential _credential;
    private readonly JmapClientOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    private string? _sessionEndpoint;
    private JmapSession? _session;

    /// <inheritdoc />
    public bool IsEntrypointResolved() => !string.IsNullOrWhiteSpace(_sessionEndpoint);

    /// <inheritdoc />
    public bool IsSessionResolved() => _session is not null;

    /// <summary>
    /// Creates a new <see cref="JmapClient"/>.
    /// </summary>
    /// <param name="http">The <see cref="HttpClient"/> used to reach the JMAP server.</param>
    /// <param name="credential">The bearer token credential to authenticate with.</param>
    /// <param name="options">Client configuration, including the JMAP host.</param>
    public JmapClient(HttpClient http, JmapTokenCredential credential, JmapClientOptions options)
    {
        _http = http;
        _credential = credential;
        _options = options;
        _jsonOptions = new JsonSerializerOptions();
    }

    private async Task ResolveEntrypointAsync(CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://{_options.Host}/{JmapClientDefaults.ApiEntrypoint}")
        {
            Headers =
            {
                { "Authorization", $"Bearer {_credential.Token}" }
            }
        };
        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        if (response.StatusCode != HttpStatusCode.Found)
            throw new JmapProtocolException(
                $"Failed to resolve session. Expected api entrypoint to return 302, instead got '{response.StatusCode}'.");

        var location = response.Headers.Location;
        if (location is null)
            throw new JmapProtocolException(
                "Failed to resolve session. Expected Location header to contain session uri, but none was present.");

        _sessionEndpoint = location.ToString();
    }

    /// <inheritdoc />
    public async Task<JmapSession> ResolveSessionAsync(CancellationToken ct)
    {
        if (!IsEntrypointResolved())
            await ResolveEntrypointAsync(ct);

        if (_session is not null)
            return _session;

        var request = new HttpRequestMessage(HttpMethod.Get, _sessionEndpoint)
        {
            Headers =
            {
                { "Authorization", $"Bearer {_credential.Token}" }
            }
        };
        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!response.IsSuccessStatusCode)
            throw new JmapProtocolException(
                $"Failed to resolve session. Session endpoint returned '{response.StatusCode}'.");

        await using var stream = await response.Content.ReadAsStreamAsync(ct);

        JmapSession? session;
        try
        {
            session = JsonSerializer.Deserialize<JmapSession>(stream, _jsonOptions);
        }
        catch (JsonException e)
        {
            throw new JmapProtocolException("Failed to resolve session. Session response could not be parsed.", e);
        }

        if (session is null)
            throw new JmapProtocolException("Failed to resolve session. Session endpoint returned an empty response body.");

        _session = session;
        return _session;
    }

    /// <inheritdoc />
    public async Task<JmapResponse> InvokeAsync(JmapRequest jmapRequest, CancellationToken ct = default)
    {
        if (!IsSessionResolved())
            await ResolveSessionAsync(ct);

        var r = new HttpRequestMessage(HttpMethod.Post, _session!.ApiUrl)
        {
            Headers =
            {
                { "Authorization", $"Bearer {_credential.Token}" }
            },
            Content = BuildContent(jmapRequest)
        };

        var httpResponse = await _http.SendAsync(r, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!httpResponse.IsSuccessStatusCode)
            throw new JmapProtocolException(
                $"Failed to invoke request. Api endpoint returned '{httpResponse.StatusCode}'.");

        var stream = await httpResponse.Content.ReadAsStreamAsync(ct);

        JmapResponse? response;
        try
        {
            response = JsonSerializer.Deserialize<JmapResponse>(stream, _jsonOptions);
        }
        catch (JsonException e)
        {
            throw new JmapProtocolException("Failed to invoke request. Response could not be parsed.", e);
        }

        if (response is null)
            throw new JmapProtocolException("Failed to invoke request. Api endpoint returned an empty response body.");

        return response;
    }
    
    private StringContent BuildContent<T>(T payload)
    {
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
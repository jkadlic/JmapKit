namespace JmapKit;

/// <summary>
/// A client for interacting with a JMAP server (<see href="https://www.rfc-editor.org/rfc/rfc8620">RFC 8620</see>).
/// </summary>
public interface IJmapClient
{
    /// <summary>
    /// Whether the JMAP session endpoint has been resolved from the well-known entrypoint.
    /// </summary>
    bool IsEntrypointResolved();

    /// <summary>
    /// Whether the JMAP session has been resolved and cached.
    /// </summary>
    bool IsSessionResolved();

    /// <summary>
    /// Resolves and caches the JMAP session, discovering the session endpoint first if needed.
    /// </summary>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The resolved <see cref="JmapSession"/>.</returns>
    /// <exception cref="JmapProtocolException">The server did not behave as the JMAP spec expects.</exception>
    Task<JmapSession> ResolveSessionAsync(CancellationToken ct);

    /// <summary>
    /// Sends a JMAP request, resolving the session first if needed.
    /// </summary>
    /// <param name="jmapRequest">The request to send.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The server's response.</returns>
    /// <exception cref="JmapProtocolException">The server did not behave as the JMAP spec expects.</exception>
    Task<JmapResponse> InvokeAsync(JmapRequest jmapRequest, CancellationToken ct = default);
}
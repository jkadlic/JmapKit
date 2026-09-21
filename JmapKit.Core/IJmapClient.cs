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

    /// <summary>
    /// Calls "<see cref="JmapCore"/>/echo". Only compatible with the '<see cref="JmapCore"/>' object, no other
    /// <see cref="IJmapObject"/> can be provided.
    /// </summary>
    /// <typeparam name="TArgs">Type of data being provided.</typeparam>
    /// <param name="args">The arguments for the call.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns></returns>
    Task<JmapMethodResponse> EchoAsync<TArgs>(TArgs args, CancellationToken ct = default);
    
    /// <summary>
    /// Calls "<typeparamref name="T"/>/get".
    /// </summary>
    /// <typeparam name="T">Type of <see cref="IJmapObject"/> being referenced.</typeparam>
    /// <param name="args">The arguments for the call.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The method response.</returns>
    /// <exception cref="JmapProtocolException">The server did not behave as the JMAP spec expects.</exception>
    Task<JmapMethodResponse> GetAsync<T>(JmapGetArguments<T> args, CancellationToken ct = default) where T : IJmapObject;

    /// <summary>
    /// Calls "<typeparamref name="T"/>/set".
    /// </summary>
    /// <typeparam name="T">Type of <see cref="IJmapObject"/> being referenced.</typeparam>
    /// <param name="args">The arguments for the call.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The method response.</returns>
    /// <exception cref="JmapProtocolException">The server did not behave as the JMAP spec expects.</exception>
    Task<JmapMethodResponse> SetAsync<T>(JmapSetArguments<T> args, CancellationToken ct = default) where T : IJmapObject;

    /// <summary>
    /// Calls "<typeparamref name="T"/>/changes".
    /// </summary>
    /// <typeparam name="T">Type of <see cref="IJmapObject"/> being referenced.</typeparam>
    /// <param name="args">The arguments for the call.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The method response.</returns>
    /// <exception cref="JmapProtocolException">The server did not behave as the JMAP spec expects.</exception>
    Task<JmapMethodResponse> ChangesAsync<T>(JmapChangesArguments<T> args, CancellationToken ct = default) where T : IJmapObject;

    /// <summary>
    /// Calls "<typeparamref name="T"/>/copy".
    /// </summary>
    /// <typeparam name="T">Type of <see cref="IJmapObject"/> being referenced.</typeparam>
    /// <param name="args">The arguments for the call.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The method response.</returns>
    /// <exception cref="JmapProtocolException">The server did not behave as the JMAP spec expects.</exception>
    Task<JmapMethodResponse> CopyAsync<T>(JmapCopyArguments<T> args, CancellationToken ct = default) where T : IJmapObject;

    /// <summary>
    /// Calls "<typeparamref name="T"/>/query".
    /// </summary>
    /// <typeparam name="T">Type of <see cref="IJmapObject"/> being referenced.</typeparam>
    /// <param name="args">The arguments for the call.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The method response.</returns>
    /// <exception cref="JmapProtocolException">The server did not behave as the JMAP spec expects.</exception>
    Task<JmapMethodResponse> QueryAsync<T>(JmapQueryArguments<T> args, CancellationToken ct = default) where T : IJmapObject;

    /// <summary>
    /// Calls "<typeparamref name="T"/>/queryChanges".
    /// </summary>
    /// <typeparam name="T">Type of <see cref="IJmapObject"/> being referenced.</typeparam>
    /// <param name="args">The arguments for the call.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The method response.</returns>
    /// <exception cref="JmapProtocolException">The server did not behave as the JMAP spec expects.</exception>
    Task<JmapMethodResponse> QueryChangesAsync<T>(JmapQueryChangesArguments<T> args, CancellationToken ct = default) where T : IJmapObject;
}
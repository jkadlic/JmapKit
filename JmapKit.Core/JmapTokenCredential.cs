namespace JmapKit;

/// <summary>
/// A bearer token credential used to authenticate requests to a JMAP server.
/// </summary>
public class JmapTokenCredential
{
    /// <summary>
    /// The bearer token sent with every request.
    /// </summary>
    public string Token { get; }

    /// <summary>
    /// Creates a credential using the <c>JMAP_TOKEN</c> environment variable.
    /// </summary>
    /// <exception cref="JmapCredentialException">The <c>JMAP_TOKEN</c> environment variable is not set.</exception>
    public JmapTokenCredential()
    {
        var token = Environment.GetEnvironmentVariable("JMAP_TOKEN");
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JMAP_TOKEN")))
            throw new JmapCredentialException("Environment variable 'JMAP_TOKEN' is required to configure JmapTokenCredential.");

        Token = token!;
    }

    /// <summary>
    /// Creates a credential with an explicit token.
    /// </summary>
    /// <param name="token">The bearer token to send with every request.</param>
    public JmapTokenCredential(string token)
    {
        Token = token;
    }
}
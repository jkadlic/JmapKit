namespace JmapKit;

/// <summary>
/// Thrown when a JMAP server doesn't behave the way the spec expects.
/// </summary>
public class JmapProtocolException : JmapException
{
    /// <summary>
    /// Creates a new <see cref="JmapProtocolException"/>.
    /// </summary>
    public JmapProtocolException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new <see cref="JmapProtocolException"/> wrapping an inner exception.
    /// </summary>
    public JmapProtocolException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
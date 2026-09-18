namespace JmapKit;

/// <summary>
/// Base type for all exceptions thrown by JmapKit.
/// </summary>
public abstract class JmapException : Exception
{
    /// <summary>
    /// Creates a new <see cref="JmapException"/>.
    /// </summary>
    protected JmapException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new <see cref="JmapException"/> wrapping an inner exception.
    /// </summary>
    protected JmapException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
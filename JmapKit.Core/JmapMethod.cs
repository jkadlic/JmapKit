namespace JmapKit;

/// <summary>
/// Defines the different JMAP methods that are available.
/// </summary>
public enum JmapMethod
{
    /// <summary>
    /// JMAP T/get method
    /// </summary>
    Get,

    /// <summary>
    /// JMAP T/set method
    /// </summary>
    Set,

    /// <summary>
    /// JMAP T/changes method
    /// </summary>
    Changes,

    /// <summary>
    /// JMAP T/copy method
    /// </summary>
    Copy,

    /// <summary>
    /// JMAP T/query method
    /// </summary>
    Query,

    /// <summary>
    /// JMAP T/queryChanges method
    /// </summary>
    QueryChanges,

    /// <summary>
    /// JMAP T/echo method
    /// </summary>
    Echo
}

/// <summary>
/// Extends the JmapMethod class with utility methods.
/// </summary>
public static class JmapMethodExtensions
{
    /// <summary>
    /// Maps a <see cref="JmapMethod"/> enum value to a JMAP method string.
    /// </summary>
    /// <param name="method">Value to map.</param>
    /// <returns>The mapped method string.</returns>
    public static string MapToMethodString(this JmapMethod method) => method switch
    {
        JmapMethod.Get => "get",
        JmapMethod.Set => "set",
        JmapMethod.Changes => "changes",
        JmapMethod.Copy => "copy",
        JmapMethod.Query => "query",
        JmapMethod.QueryChanges => "queryChanges",
        JmapMethod.Echo => "echo",
        _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
    };
}

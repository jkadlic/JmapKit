namespace JmapKit;

/// <summary>
/// The JMAP Core capability (<see href="https://www.rfc-editor.org/rfc/rfc8620#section-2">RFC 8620 §2</see>),
/// required on every JMAP request.
/// </summary>
public static class JmapCoreCapability
{
    /// <summary>
    /// <c>urn:ietf:params:jmap:core</c>
    /// </summary>
    public static readonly JmapCapability Core = new("urn:ietf:params:jmap:core");
}
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JmapKit;

/// <summary>
/// A single method call within a <see cref="JmapRequest"/>. Serializes as the positional
/// <c>[name, arguments, callId]</c> tuple JMAP expects.
/// </summary>
[JsonConverter(typeof(JmapMethodInvocationConverter))]
public sealed class JmapMethodInvocation
{
    /// <summary>
    /// The method name, e.g. <c>Core/echo</c>
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The method's arguments.
    /// </summary>
    public JsonElement Arguments { get; }

    /// <summary>
    /// A client-assigned id used to match this call to its <see cref="JmapMethodResponse"/>.
    /// </summary>
    public string CallId { get; }

    private JmapMethodInvocation(string name, JsonElement arguments, string callId)
    {
        Name = name;
        Arguments = arguments;
        CallId = callId;
    }

    /// <summary>
    /// Creates a <see cref="JmapMethodInvocation"/> calling "<typeparamref name="T"/>/<paramref name="method"/>".
    /// </summary>
    /// <typeparam name="T">Type of <see cref="IJmapObject"/> being referenced.</typeparam>
    /// <param name="method">The method to call.</param>
    /// <param name="arguments">The method's arguments.</param>
    /// <param name="callId">A client-assigned id used to match this call to its <see cref="JmapMethodResponse"/>.</param>
    /// <exception cref="JmapUnsupportedMethodException">
    /// <typeparamref name="T"/> does not declare support for <paramref name="method"/> via
    /// <see cref="IJmapObject.SupportedMethods"/>.
    /// </exception>
    public static JmapMethodInvocation Create<T>(JmapMethod method, JsonElement arguments, string callId)
        where T : IJmapObject
    {
        if (!T.SupportedMethods.Contains(method))
            throw new JmapUnsupportedMethodException(T.JmapName, method);

        return new JmapMethodInvocation($"{T.JmapName}/{method.MapToMethodString()}", arguments, callId);
    }
}

/// <summary>
/// Writes a <see cref="JmapMethodInvocation"/> as a <c>[name, arguments, callId]</c> tuple.
/// </summary>
public class JmapMethodInvocationConverter : JsonConverter<JmapMethodInvocation>
{
    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown; see remarks on <see cref="JmapMethodInvocationConverter"/>.</exception>
    public override JmapMethodInvocation Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        throw new NotSupportedException("MethodInvocation is write-only.");
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        JmapMethodInvocation value,
        JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteStringValue(value.Name);
        value.Arguments.WriteTo(writer);
        writer.WriteStringValue(value.CallId);
        writer.WriteEndArray();
    }
}
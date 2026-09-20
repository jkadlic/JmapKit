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
    public required string Name { get; init; }

    /// <summary>
    /// The method's arguments.
    /// </summary>
    public required JsonElement Arguments { get; init; }

    /// <summary>
    /// A client-assigned id used to match this call to its <see cref="JmapMethodResponse"/>.
    /// </summary>
    public required string CallId { get; init; }
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
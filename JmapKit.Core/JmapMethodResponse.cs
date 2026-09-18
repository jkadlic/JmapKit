using System.Text.Json;
using System.Text.Json.Serialization;

namespace JmapKit;

/// <summary>
/// A single method response within a <see cref="JmapResponse"/>. Read from the positional
/// <c>[name, arguments, callId]</c> tuple JMAP sends.
/// </summary>
[JsonConverter(typeof(JmapMethodResponseConverter))]
public class JmapMethodResponse
{
    /// <summary>
    /// The method name, matching the call it responds to (or <c>error</c> on failure).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The method's result arguments.
    /// </summary>
    public required Dictionary<string, object?> Arguments { get; init; }

    /// <summary>
    /// The client-assigned id from the <see cref="JmapMethodInvocation"/> this responds to.
    /// </summary>
    public required string CallId { get; init; }
}

/// <summary>
/// Reads a <c>[name, arguments, callId]</c> tuple into a <see cref="JmapMethodResponse"/>.
/// </summary>
public class JmapMethodResponseConverter : JsonConverter<JmapMethodResponse>
{
    /// <inheritdoc />
    public override JmapMethodResponse Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array for method response tuple.");

        reader.Read();
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected method name as first element of method response tuple.");
        var name = reader.GetString()!;

        reader.Read();
        var arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(ref reader, options);
        if (arguments is null)
            throw new JsonException("Expected arguments object as second element of method response tuple.");

        reader.Read();
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected call id as third element of method response tuple.");
        var callId = reader.GetString()!;

        reader.Read();
        if (reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected end of array for method response tuple.");

        return new JmapMethodResponse { Name = name, Arguments = arguments, CallId = callId };
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown; <see cref="JmapMethodResponse"/> is read-only.</exception>
    public override void Write(
        Utf8JsonWriter writer,
        JmapMethodResponse value,
        JsonSerializerOptions options)
    {
        throw new NotSupportedException("MethodResponse is read-only.");
    }
}
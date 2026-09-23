using System.Diagnostics.CodeAnalysis;
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
    public required JsonElement Arguments { get; init; }

    /// <summary>
    /// The client-assigned id from the <see cref="JmapMethodInvocation"/> this responds to.
    /// </summary>
    public required string CallId { get; init; }

    /// <summary>
    /// Whether the server returned a method-level error (RFC 8620 §3.6.1) instead of a normal result.
    /// </summary>
    public bool IsError => Name == "error";

    /// <summary>
    /// The options this response was read with, captured by <see cref="JmapMethodResponseConverter"/> so
    /// that deserializing the payload uses the same configuration as the envelope.
    /// </summary>
    internal JsonSerializerOptions? SerializerOptions { get; init; }

    /// <summary>
    /// Deserializes <see cref="Arguments"/> into <typeparamref name="T"/> if this call succeeded, or into a
    /// <see cref="JmapMethodError"/> if the server returned an error in its place.
    /// </summary>
    /// <remarks>
    /// Uses the client's configured <see cref="JsonSerializerOptions"/>, so the payload is read exactly as
    /// the envelope was. Configure them through <c>AddJmapClient</c>, or give a data type its own
    /// <see cref="JsonConverterAttribute"/>.
    /// </remarks>
    /// <returns>True if deserialized into <paramref name="value"/>; false if deserialized into <paramref name="error"/>.</returns>
    public bool TryDeserialize<T>(
        [NotNullWhen(true)] out T? value,
        [NotNullWhen(false)] out JmapMethodError? error)
    {
        var options = SerializerOptions ?? JmapJson.Default;

        if (IsError)
        {
            value = default;
            error = Arguments.Deserialize<JmapMethodError>(options)!;
            return false;
        }

        value = Arguments.Deserialize<T>(options)!;
        error = null;
        return true;
    }
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
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected arguments object as second element of method response tuple.");
        var arguments = JsonElement.ParseValue(ref reader);

        reader.Read();
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected call id as third element of method response tuple.");
        var callId = reader.GetString()!;

        reader.Read();
        if (reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected end of array for method response tuple.");

        return new JmapMethodResponse
        {
            Name = name,
            Arguments = arguments,
            CallId = callId,
            SerializerOptions = options,
        };
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
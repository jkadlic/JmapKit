using System.Text.Json;
using System.Text.Json.Serialization;

namespace JmapKit;

/// <summary>
/// Reads an untyped JSON value into plain CLR types (long/double, string, bool, null,
/// List&lt;object?&gt;, Dictionary&lt;string, object?&gt;) instead of the default
/// <see cref="JsonElement"/> boxing, so JSON types never leak past the JMAP client boundary.
/// </summary>
internal sealed class JmapObjectConverter : JsonConverter<object?>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Null => null,
            JsonTokenType.Number => reader.TryGetInt64(out var l) ? l : reader.GetDouble(),
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.StartObject => JsonSerializer.Deserialize<Dictionary<string, object?>>(ref reader, options),
            JsonTokenType.StartArray => JsonSerializer.Deserialize<List<object?>>(ref reader, options),
            _ => throw new JsonException($"Unsupported token type '{reader.TokenType}' when reading a JMAP value.")
        };

    public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}

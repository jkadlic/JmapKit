using System.Text.Json;
using System.Text.Json.Serialization;

namespace JmapKit;

/// <summary>
/// A JMAP capability, identified by its URI (e.g. <c>urn:ietf:params:jmap:core</c>).
/// </summary>
/// <param name="Uri">The capability's URI.</param>
[JsonConverter(typeof(JmapCapabilityConverter))]
public readonly record struct JmapCapability(string Uri);

/// <summary>
/// Serializes a <see cref="JmapCapability"/> as its bare URI string.
/// </summary>
public class JmapCapabilityConverter : JsonConverter<JmapCapability>
{
    /// <inheritdoc />
    public override JmapCapability Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var uri = reader.GetString();
        if (uri is null)
            throw new JsonException("Expected capability uri to be a non-null string.");

        return new JmapCapability(uri);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, JmapCapability value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Uri);
}
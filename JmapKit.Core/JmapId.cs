using System.Text.Json;
using System.Text.Json.Serialization;

namespace JmapKit;

/// <summary>
/// A JMAP id, used to identify all resources in a JMAP server. A JMAP response, returned from the API endpoint (<see href="https://www.rfc-editor.org/rfc/rfc8620.html#section-1.2">RFC 8620 §1.2</see>).
/// </summary>
[JsonConverter(typeof(JmapIdConverter))]
public readonly struct JmapId : IEquatable<JmapId>, IComparable<JmapId>
{
    /// <summary>
    /// The minimum length in octets for an id.
    /// </summary>
    public const int MinLengthOctets = 1;
    
    /// <summary>
    /// The maximum length in octets for an id.
    /// </summary>
    public const int MaxLengthOctets = 255;

    private readonly string _value;

    private JmapId(string value) => _value = value;

    /// <summary>
    /// Attempts to parse a string into a <see cref="JmapId"/>. Populates the id out var and returns true if the
    /// operation was successful, otherwise returns false.
    /// </summary>
    /// <param name="input">String to parse.</param>
    /// <param name="result">Resulting <see cref="JmapId"/> if successful.</param>
    /// <returns>If parsing was a success.</returns>
    public static bool TryParse(string? input, out JmapId result)
    {
        if (input?.Length is >= MinLengthOctets and <= MaxLengthOctets && IsValidContent(input))
        {
            result = new JmapId(input);
            return true;
        }

        result = default;
        return false;
    }
    
    /// <summary>
    /// Attempts to parse a string into a <see cref="JmapId"/> and throws on failure. Returns the parsed
    /// <see cref="JmapId"/> if the operation was successful, otherwise throws <see cref="FormatException"/>.
    /// </summary>
    /// <param name="input">String to parse.</param>
    /// <returns>The parsed <see cref="JmapId"/> if successful.</returns>
    /// <exception cref="FormatException">Thrown if the provided string failed to parse into a <see cref="JmapId"/>.</exception>
    public static JmapId Parse(string input)
    {
        if (!TryParse(input, out var result))
        {
            throw new FormatException(
                $"'{input}' is not a valid Id. Must be {MinLengthOctets}-{MaxLengthOctets} " +
                "octets long and contain only 'A'-'Z', 'a'-'z', '0'-'9', '-', or '_'.");
        }

        return result;
    }

    private static bool IsValidContent(string input)
    {
        foreach (var c in input)
        {
            var isValid =
                (c >= 'A' && c <= 'Z') ||
                (c >= 'a' && c <= 'z') ||
                (c >= '0' && c <= '9') ||
                c == '-' ||
                c == '_';
 
            if (!isValid)
            {
                return false;
            }
        }
 
        return true;
    }
    
    /// inheritdoc
    public override string ToString() => _value ?? string.Empty;
    
    /// <summary>
    /// Maps an attempted cast form <see cref="JmapId"/> to <see cref="string"/> to a ToString call.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static implicit operator string(JmapId id) => id.ToString();
    
    /// <summary>
    /// Maps an attempted cast from <see cref="string"/> to <see cref="JmapId"/> to an explicit parse operation. Needs
    /// an explicit mapping because not all strings are valid JMAP id's.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static explicit operator JmapId(string input) => Parse(input);

    /// inheritdoc
    public bool Equals(JmapId other) => string.Equals(_value, other._value, StringComparison.Ordinal);

    /// inheritdoc
    public override bool Equals(object? obj) => obj is JmapId other && Equals(other);

    /// inheritdoc
    public override int GetHashCode() => _value?.GetHashCode(StringComparison.Ordinal) ?? 0;

    /// inheritdoc
    public int CompareTo(JmapId other) => string.CompareOrdinal(_value, other._value);
}

/// <summary>
/// Serializes a <see cref="JmapId"/> as its bare string value.
/// </summary>
public class JmapIdConverter : JsonConverter<JmapId>
{
    /// <inheritdoc />
    public override JmapId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (!JmapId.TryParse(value, out var id))
            throw new JsonException($"'{value}' is not a valid JMAP id.");

        return id;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, JmapId value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}

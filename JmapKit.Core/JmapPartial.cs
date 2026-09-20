using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace JmapKit;

/// <summary>
/// A sparse subset of <typeparamref name="T"/>'s properties, as returned for created or updated objects from
/// "Foo/set" (<see href="https://www.rfc-editor.org/rfc/rfc8620#section-5.3">RFC 8620 §5.3</see>).
/// </summary>
[JsonConverter(typeof(JmapPartialConverterFactory))]
public sealed class JmapPartial<T>
{
	private readonly JsonObject _properties;

	internal JmapPartial(JsonObject properties) => _properties = properties;

	/// <summary>
	/// Merges the properties returned by the server onto <paramref name="original"/>, producing the complete
	/// <typeparamref name="T"/> as it now exists on the server.
	/// </summary>
	/// <param name="original">
	/// The object as the client last knew it, e.g. the object passed to <c>Create</c>.
	/// </param>
	/// <param name="options">
	/// The <see cref="JsonSerializerOptions"/> used to communicate with the JMAP server.
	/// </param>
	/// <returns>A complete <typeparamref name="T"/> combining <paramref name="original"/> with the server's changes.</returns>
	public T MergeOnto(T original, JsonSerializerOptions options)
	{
		var node = JsonSerializer.SerializeToNode(original, options)?.AsObject()
			?? throw new JsonException($"Failed to serialize '{typeof(T)}' while merging a {nameof(JmapPartial<T>)}.");

		foreach (var (name, value) in _properties)
			node[name] = value?.DeepClone();

		return node.Deserialize<T>(options)!;
	}
}

/// <summary>
/// Creates <see cref="JmapPartialConverter{T}"/> instances for closed <see cref="JmapPartial{T}"/> types.
/// </summary>
public sealed class JmapPartialConverterFactory : JsonConverterFactory
{
	/// <inheritdoc />
	public override bool CanConvert(Type typeToConvert) =>
		typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(JmapPartial<>);

	/// <inheritdoc />
	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		var itemType = typeToConvert.GetGenericArguments()[0];
		return (JsonConverter)Activator.CreateInstance(typeof(JmapPartialConverter<>).MakeGenericType(itemType))!;
	}
}

/// <summary>
/// Reads a JSON object into a <see cref="JmapPartial{T}"/> without requiring it to satisfy
/// <typeparamref name="T"/>'s own shape, since the server only ever sends a subset of
/// <typeparamref name="T"/>'s properties here.
/// </summary>
public sealed class JmapPartialConverter<T> : JsonConverter<JmapPartial<T>>
{
	/// <inheritdoc />
	public override JmapPartial<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (JsonNode.Parse(ref reader) is not JsonObject properties)
			throw new JsonException($"Expected a JSON object for a {nameof(JmapPartial<T>)} value.");

		return new JmapPartial<T>(properties);
	}

	/// <inheritdoc />
	/// <exception cref="NotSupportedException">Always thrown; <see cref="JmapPartial{T}"/> is read-only.</exception>
	public override void Write(Utf8JsonWriter writer, JmapPartial<T> value, JsonSerializerOptions options)
	{
		throw new NotSupportedException($"{nameof(JmapPartial<T>)} is read-only.");
	}
}
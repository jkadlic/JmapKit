using System.Text.Json.Serialization;

namespace JmapKit;

/// <summary>
/// Identifies a record that has been added to a query's results, and the position it now occupies, as
/// returned from "/queryChanges" method calls
/// (<see href="https://www.rfc-editor.org/rfc/rfc8620#section-5.6">RFC 8620 §5.6</see>).
/// </summary>
public sealed record JmapAddedItem
{
	/// <summary>
	/// The id of the record added to the query's results.
	/// </summary>
	[JsonPropertyName("id")]
	public required JmapId Id { get; init; }

	/// <summary>
	/// The zero-based index of <see cref="Id"/> within the full result set.
	/// </summary>
	[JsonPropertyName("index")]
	public required long Index { get; init; }
}
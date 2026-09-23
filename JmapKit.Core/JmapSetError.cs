using System.Text.Json.Serialization;

namespace JmapKit;

/// <summary>
/// Defines an error returned from /set operations.
/// </summary>
public sealed record JmapSetError
{
	/// <summary>
	/// Type of error encountered.
	/// </summary>
	[JsonPropertyName("type")]
	public required string Type { get; init; }

	/// <summary>
	/// Description of error encountered.
	/// </summary>
	[JsonPropertyName("description")]
	public string? Description { get; init; }
}
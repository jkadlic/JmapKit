namespace JmapKit;

/// <summary>
/// Defines an error returned from /set operations.
/// </summary>
public sealed record JmapSetError
{
	/// <summary>
	/// Type of error encountered.
	/// </summary>
	public required string Type { get; init; }

	/// <summary>
	/// Description of error encountered.
	/// </summary>
	public string? Description { get; init; }
}
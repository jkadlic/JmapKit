namespace JmapKit;

/// <summary>
/// Describes the order in which to sort records returned by a "/query" or "/queryChanges" method call
/// (<see href="https://www.rfc-editor.org/rfc/rfc8620#section-5.5">RFC 8620 §5.5</see>).
/// </summary>
public sealed record JmapComparator
{
	/// <summary>
	/// The name of the property on which to sort.
	/// </summary>
	public required string Property { get; init; }

	/// <summary>
	/// If true, sort in ascending order with respect to <see cref="Property"/>; if false, sort in descending
	/// order. Defaults to true.
	/// </summary>
	public bool IsAscending { get; init; } = true;

	/// <summary>
	/// The identifier, as registered in the collation registry defined in
	/// <see href="https://www.rfc-editor.org/rfc/rfc4790">RFC 4790</see>, for the algorithm used to compare
	/// <see cref="Property"/> values when sorting. If omitted, the default collation for the property is used.
	/// </summary>
	public string? Collation { get; init; }
}
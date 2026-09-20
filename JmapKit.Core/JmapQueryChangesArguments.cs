using System.Text.Json;

namespace JmapKit;

/// <summary>
/// Defines the arguments that can be provided to /queryChanges method calls
/// (<see href="https://www.rfc-editor.org/rfc/rfc8620#section-5.6">RFC 8620 §5.6</see>).
/// </summary>
/// <typeparam name="T">Type of <see cref="IJmapObject"/> being referenced.</typeparam>
public sealed record JmapQueryChangesArguments<T>
	where T : IJmapObject
{
	/// <summary>
	/// The id of the account to use.
	/// </summary>
	public required JmapId AccountId { get; init; }

	/// <summary>
	/// The same filter as passed to "<see cref="T"/>/query"; see
	/// <see cref="JmapQueryArguments{T}.Filter"/>.
	/// </summary>
	public JsonElement? Filter { get; init; }

	/// <summary>
	/// The same sort as passed to "<see cref="T"/>/query"; see <see cref="JmapQueryArguments{T}.Sort"/>.
	/// </summary>
	public JmapComparator[]? Sort { get; init; }

	/// <summary>
	/// The current state of the query in the client, as returned in a previous
	/// "<see cref="T"/>/query" or "<see cref="T"/>/queryChanges" response.
	/// </summary>
	public required string SinceQueryState { get; init; }

	/// <summary>
	/// The maximum number of changes to return, or null (the default) for no limit. If the number of changes
	/// exceeds this, the server returns a "tooManyChanges" error instead.
	/// </summary>
	public long? MaxChanges { get; init; }

	/// <summary>
	/// If supplied, the client is only interested in changes up to and including this id being added to or
	/// removed from the results; the server may exclude changes to results after this id in its response.
	/// Null (the default) means the client is interested in all changes.
	/// </summary>
	public JmapId? UpToId { get; init; }

	/// <summary>
	/// If true, the response will include <see cref="JmapQueryChangesResponse{T}.Total"/>, giving the total
	/// number of results matching the query after applying the changes. Defaults to false, since this may be
	/// expensive for the server to calculate.
	/// </summary>
	public bool CalculateTotal { get; init; }
}

/// <summary>
/// Defines the response that will be returned from /queryChanges method calls.
/// </summary>
/// <typeparam name="T">Type of <see cref="IJmapObject"/> being referenced.</typeparam>
public sealed record JmapQueryChangesResponse<T>
	where T : IJmapObject
{
	/// <summary>
	/// The id of the account used for the call.
	/// </summary>
	public required JmapId AccountId { get; init; }

	/// <summary>
	/// This is the <see cref="JmapQueryChangesArguments{T}.SinceQueryState"/> echoed back; the state of the
	/// query before these changes were applied.
	/// </summary>
	public required string OldQueryState { get; init; }

	/// <summary>
	/// The state the query will be in after applying the set of changes to the old state.
	/// </summary>
	public required string NewQueryState { get; init; }

	/// <summary>
	/// The total number of <see cref="T"/> objects in the results, ignoring position and limit. Only present
	/// if <see cref="JmapQueryChangesArguments{T}.CalculateTotal"/> was true.
	/// </summary>
	public long? Total { get; init; }

	/// <summary>
	/// The ids that were in the old query results but are not in the new results. A record that changed
	/// position is represented here and in <see cref="Added"/>, rather than being omitted.
	/// </summary>
	public required JmapId[] Removed { get; init; }

	/// <summary>
	/// The ids that were not in the old query results but are in the new results, along with the position
	/// they now occupy, in the same order as they appear in the new results.
	/// </summary>
	public required JmapAddedItem[] Added { get; init; }
}
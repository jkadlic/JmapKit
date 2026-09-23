using System.Text.Json;
using System.Text.Json.Serialization;

namespace JmapKit;

/// <summary>
/// Defines the arguments that can be provided to /query method calls
/// (<see href="https://www.rfc-editor.org/rfc/rfc8620#section-5.5">RFC 8620 §5.5</see>).
/// </summary>
/// <typeparam name="T">Type of <see cref="IJmapObject"/> being referenced.</typeparam>
public sealed record JmapQueryArguments<T>
	where T : IJmapObject
{
	/// <summary>
	/// The id of the account to use.
	/// </summary>
	[JsonPropertyName("accountId")]
	public required JmapId AccountId { get; init; }

	/// <summary>
	/// Determines which <typeparamref name="T"/> objects are included in the results, as either a FilterOperator
	/// (combining other filters with "AND"/"OR"/"NOT") or a FilterCondition whose supported properties are
	/// defined by <typeparamref name="T"/>. Null (the default) means all objects are included.
	/// </summary>
	[JsonPropertyName("filter")]
	public JsonElement? Filter { get; init; }

	/// <summary>
	/// Lists the properties to sort the results by, in order of precedence. If null or empty, the server
	/// picks a default, stable order.
	/// </summary>
	[JsonPropertyName("sort")]
	public JmapComparator[]? Sort { get; init; }

	/// <summary>
	/// The zero-based index of the first result to return. If negative, indexes from the end of the full
	/// result set, with -1 meaning the last result. Defaults to 0.
	/// </summary>
	[JsonPropertyName("position")]
	public long Position { get; init; }

	/// <summary>
	/// The id of a record in the results, used together with <see cref="AnchorOffset"/> to determine the
	/// starting position instead of using <see cref="Position"/> directly. Null (the default) means
	/// <see cref="Position"/> is used unmodified.
	/// </summary>
	[JsonPropertyName("anchor")]
	public JmapId? Anchor { get; init; }

	/// <summary>
	/// The index of the first result to return, relative to <see cref="Anchor"/>. May be negative to select
	/// results before the anchor. Only used when <see cref="Anchor"/> is set. Defaults to 0.
	/// </summary>
	[JsonPropertyName("anchorOffset")]
	public long AnchorOffset { get; init; }

	/// <summary>
	/// The maximum number of results to return, or null (the default) to let the server decide, which may be
	/// further limited by a server-enforced maximum.
	/// </summary>
	[JsonPropertyName("limit")]
	public long? Limit { get; init; }

	/// <summary>
	/// If true, the response will include <see cref="JmapQueryResponse{T}.Total"/>, giving the total number
	/// of results found, ignoring <see cref="Position"/> and <see cref="Limit"/>. Defaults to false, since
	/// this may be expensive for the server to calculate.
	/// </summary>
	[JsonPropertyName("calculateTotal")]
	public bool CalculateTotal { get; init; }
}

/// <summary>
/// Defines the response that will be returned from /query method calls.
/// </summary>
/// <typeparam name="T">Type of <see cref="IJmapObject"/> being referenced.</typeparam>
public sealed record JmapQueryResponse<T>
	where T : IJmapObject
{
	/// <summary>
	/// The id of the account used for the call.
	/// </summary>
	[JsonPropertyName("accountId")]
	public required JmapId AccountId { get; init; }

	/// <summary>
	/// A string encoding the current state of the query on the server. This is passed as
	/// <see cref="JmapQueryChangesArguments{T}.SinceQueryState"/> on a later "<typeparamref name="T"/>/queryChanges"
	/// call to fetch only what has changed.
	/// </summary>
	[JsonPropertyName("queryState")]
	public required string QueryState { get; init; }

	/// <summary>
	/// If true, the server supports calling "<typeparamref name="T"/>/queryChanges" for this query going forward; the
	/// meaning of this filter/sort combination may not otherwise be trackable over time (e.g. it depends on
	/// the client's local time).
	/// </summary>
	[JsonPropertyName("canCalculateChanges")]
	public required bool CanCalculateChanges { get; init; }

	/// <summary>
	/// The zero-based index, within the full result set, of the first id in <see cref="Ids"/>.
	/// </summary>
	[JsonPropertyName("position")]
	public required long Position { get; init; }

	/// <summary>
	/// The ids of the <typeparamref name="T"/> objects matching the query, in the requested sort order, starting from
	/// <see cref="Position"/> and limited to at most <see cref="JmapQueryArguments{T}.Limit"/> entries.
	/// </summary>
	[JsonPropertyName("ids")]
	public required JmapId[] Ids { get; init; }

	/// <summary>
	/// The total number of <typeparamref name="T"/> objects in the results, ignoring position and limit. Only present
	/// if <see cref="JmapQueryArguments{T}.CalculateTotal"/> was true.
	/// </summary>
	[JsonPropertyName("total")]
	public long? Total { get; init; }

	/// <summary>
	/// The limit enforced by the server, if it differs from the limit given (or omitted) in the request, e.g.
	/// because the request had no limit and the server clamped it, or the requested limit exceeded the
	/// server's maximum.
	/// </summary>
	[JsonPropertyName("limit")]
	public long? Limit { get; init; }
}
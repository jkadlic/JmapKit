using System.Text.Json.Serialization;

namespace JmapKit;

/// <summary>
/// Defines the arguments that can be provided to /changes method calls.
/// </summary>
/// <typeparam name="T">Type of <see cref="IJmapObject"/> being referenced.</typeparam>
public sealed record JmapChangesArguments<T>
	where T : IJmapObject
{
	/// <summary>
	/// The id of the account to use.
	/// </summary>
	[JsonPropertyName("accountId")]
	public required JmapId AccountId { get; init; }
	
	/// <summary>
	/// The current state of the client.
	/// </summary>
	[JsonPropertyName("sinceState")]
	public required string SinceState { get; init; }

	/// <summary>
	/// The maximum number of ids to return in the response.
	/// </summary>
	[JsonPropertyName("maxChanges")]
	public long? MaxChanges { get; init; }
}

/// <summary>
/// Defines the response that will be returned from /changes method calls.
/// </summary>
/// <typeparam name="T">Type of <see cref="IJmapObject"/> being referenced.</typeparam>
public sealed record JmapChangesResponse<T>
	where T : IJmapObject
{
	/// <summary>
	/// The id of the account used for the call.
	/// </summary>
	[JsonPropertyName("accountId")]
	public required JmapId AccountId { get; init; }

	/// <summary>
	/// This is the initial state of the server before the request.
	/// </summary>
	[JsonPropertyName("oldState")]
	public required string OldState { get; init; }

	/// <summary>
	/// This is the state the client will be in after applying the set of changes to the old state.
	/// </summary>
	[JsonPropertyName("newState")]
	public required string NewState { get; init; }

	/// <summary>
	/// If true, more changes can be retrieved from the server. If false, "newState" is the current server state.
	/// </summary>
	[JsonPropertyName("hasMoreChanges")]
	public required bool HasMoreChanges { get; init; }

	/// <summary>
	/// An array of ids for records that have been created since the old state.
	/// </summary>
	[JsonPropertyName("created")]
	public required JmapId[] Created { get; init; }
	
	/// <summary>
	/// An array of ids for records that have been updated since the old state.
	/// </summary>
	[JsonPropertyName("updated")]
	public required JmapId[] Updated { get; init; }
	
	/// <summary>
	/// An array of ids for records that have been destroyed since the old state.
	/// </summary>
	[JsonPropertyName("destroyed")]
	public required JmapId[] Destroyed { get; init; }
}
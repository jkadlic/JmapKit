using System.Text.Json.Serialization;

namespace JmapKit;

/// <summary>
/// Defines the arguments that can be provided to /get method calls.
/// </summary>
/// <typeparam name="T">Type of <see cref="IJmapObject"/> being referenced.</typeparam>
public sealed record JmapGetArguments<T>
	where T : IJmapObject
{
	/// <summary>
	/// The id of the account to use.
	/// </summary>
	[JsonPropertyName("accountId")]
	public required JmapId AccountId { get; init; }

	/// <summary>
	/// The ids of the <typeparamref name="T"/> objects to return.
	/// </summary>
	[JsonPropertyName("ids")]
	public JmapId[]? Ids { get; init; }
	
	/// <summary>
	/// If supplied, only the properties listed in the array are returned for each <typeparamref name="T"/> object.
	/// </summary>
	[JsonPropertyName("properties")]
	public string[]? Properties { get; init; }
}

/// <summary>
/// Defines the response that will be returned from /get method calls.
/// </summary>
/// <typeparam name="T">Type of <see cref="IJmapObject"/> being referenced.</typeparam>
public sealed record JmapGetResponse<T>
	where T : IJmapObject
{
	/// <summary>
	/// The id of the account used for the call.
	/// </summary>
	[JsonPropertyName("accountId")]
	public required JmapId AccountId { get; init; }
	
	/// <summary>
	/// A string representing the state on the server for all the data of type <typeparamref name="T"/> in the account.
	/// </summary>
	[JsonPropertyName("state")]
	public required string State { get; init; }
	
	/// <summary>
	/// An array of the <typeparamref name="T"/> objectes requested.
	/// </summary>
	[JsonPropertyName("list")]
	public required T[] List { get; init; }
	
	/// <summary>
	/// An array of the referenced <typeparamref name="T"/> objects that do not exist.
	/// </summary>
	[JsonPropertyName("notFound")]
	public required JmapId[] NotFound { get; init; }
}
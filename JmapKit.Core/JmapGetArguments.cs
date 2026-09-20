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
	public required JmapId AccountId { get; init; }

	/// <summary>
	/// The ids of the <see cref="T"/> objects to return.
	/// </summary>
	public JmapId[]? Ids { get; init; }
	
	/// <summary>
	/// If supplied, only the properties listed in the array are returned for each <see cref="T"/> object.
	/// </summary>
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
	public required JmapId AccountId { get; init; }
	
	/// <summary>
	/// A string representing the state on the server for all the data of type <see cref="T"/> in the account.
	/// </summary>
	public required string State { get; init; }
	
	/// <summary>
	/// An array of the <see cref="T"/> objectes requested.
	/// </summary>
	public required T[] List { get; init; }
	
	/// <summary>
	/// An array of the referenced <see cref="T"/> objects that do not exist.
	/// </summary>
	public required JmapId[] NotFound { get; init; }
}
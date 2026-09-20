namespace JmapKit;

/// <summary>
/// Defines the arguments that can be provided to /copy method calls
/// (<see href="https://www.rfc-editor.org/rfc/rfc8620#section-5.4">RFC 8620 §5.4</see>).
/// </summary>
/// <typeparam name="T">Type of <see cref="IJmapObject"/> being referenced.</typeparam>
public sealed record JmapCopyArguments<T>
	where T : IJmapObject
{
	/// <summary>
	/// The id of the account to copy records from.
	/// </summary>
	public required JmapId FromAccountId { get; init; }

	/// <summary>
	/// A state string as returned by "<see cref="T"/>/get" for the account referenced by
	/// <see cref="FromAccountId"/>. If supplied, the string must match the current state of that account for
	/// the request to be processed; otherwise the whole method call is rejected with a "stateMismatch" error.
	/// </summary>
	public string? IfFromInState { get; init; }

	/// <summary>
	/// The id of the account to copy records into.
	/// </summary>
	public required JmapId AccountId { get; init; }

	/// <summary>
	/// A state string as returned by "<see cref="T"/>/get" for the account referenced by <see cref="AccountId"/>.
	/// If supplied, the string must match the current state of that account for the request to be processed;
	/// otherwise the whole method call is rejected with a "stateMismatch" error.
	/// </summary>
	public string? IfInState { get; init; }

	/// <summary>
	/// A map of the creation id to a <see cref="T"/> object. Each object must reference the id of the record
	/// in the <see cref="FromAccountId"/> account to copy from, and may set other properties to override the
	/// values of the copy.
	/// </summary>
	public required Dictionary<JmapId, T> Create { get; init; }

	/// <summary>
	/// If true, the server will attempt to destroy the original records that were successfully copied out of
	/// the <see cref="FromAccountId"/> account once the copy completes, equivalent to a subsequent
	/// "<see cref="T"/>/set" call with those ids in its "destroy" argument. Defaults to false.
	/// </summary>
	public bool OnSuccessDestroyOriginal { get; init; }

	/// <summary>
	/// A state string as returned by "<see cref="T"/>/get" for the account referenced by
	/// <see cref="FromAccountId"/>. Only used when <see cref="OnSuccessDestroyOriginal"/> is true; if supplied
	/// and it does not match the current state of that account, the whole method call is rejected with a
	/// "stateMismatch" error before any copy is attempted.
	/// </summary>
	public string? DestroyFromIfInState { get; init; }
}

/// <summary>
/// Defines the response that will be returned from /copy method calls.
/// </summary>
/// <typeparam name="T">Type of <see cref="IJmapObject"/> being referenced.</typeparam>
public sealed record JmapCopyResponse<T>
	where T : IJmapObject
{
	/// <summary>
	/// The id of the account records were copied from.
	/// </summary>
	public required JmapId FromAccountId { get; init; }

	/// <summary>
	/// The id of the account records were copied into.
	/// </summary>
	public required JmapId AccountId { get; init; }

	/// <summary>
	/// The state string that would have been returned by "<see cref="T"/>/get" on the destination account
	/// before making the requested changes, or null if the server doesn't know what the previous state was.
	/// </summary>
	public required string? OldState { get; init; }

	/// <summary>
	/// The state string that will now be returned by "<see cref="T"/>/get" on the destination account.
	/// </summary>
	public required string NewState { get; init; }

	/// <summary>
	/// A map of the creation id to a <see cref="JmapPartial{T}"/> containing any properties of the copied
	/// <see cref="T"/> that differ from the source record.
	///
	/// Null if no <see cref="T"/> objects were successfully copied.
	/// </summary>
	public Dictionary<JmapId, JmapPartial<T>>? Created { get; init; }

	/// <summary>
	/// A map of the creation id to a <see cref="JmapSetError"/> object for each <see cref="T"/> that
	/// failed to be copied.
	/// </summary>
	public Dictionary<JmapId, JmapSetError>? NotCreated { get; init; }
}
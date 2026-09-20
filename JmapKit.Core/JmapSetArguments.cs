namespace JmapKit;

/// <summary>
/// Defines the arguments that can be provided to /set method calls.
/// </summary>
/// <typeparam name="T">Type of <see cref="IJmapObject"/> being referenced.</typeparam>
public sealed record JmapSetArguments<T>
	where T : IJmapObject
{
	/// <summary>
	/// The id of the account to use.
	/// </summary>
	public required JmapId AccountId { get; init; }

	/// <summary>
	/// A state string as returned by "<see cref="T"/>/get". If supplied, the string must match the
	/// current state on the server for the request to be processed; otherwise the whole method call is
	/// rejected with a "stateMismatch" error.
	/// </summary>
	public string? IfInState { get; init; }

	/// <summary>
	/// A map of a creation id to <see cref="T"/> objects, or null if no objects are to be created.
	/// </summary>
	public Dictionary<JmapId, T>? Create { get; init; }

	/// <summary>
	/// A map of the id of an existing <see cref="T"/> object to a Patch object describing the changes to apply.
	/// </summary>
	public Dictionary<JmapId, object>? Update { get; init; }

	/// <summary>
	/// A list of ids for <see cref="T"/> objects to permanently delete.
	/// </summary>
	public List<JmapId>? Destroy { get; init; }
}

/// <summary>
/// Defines the response that will be returned from /set method calls.
/// </summary>
/// <typeparam name="T">Type of <see cref="IJmapObject"/> being referenced.</typeparam>
public sealed record JmapSetResponse<T>
	where T : IJmapObject
{
	/// <summary>
	/// The id of the account used for the call.
	/// </summary>
	public required JmapId AccountId { get; init; }

	/// <summary>
	/// The state string that would have been returned by "<see cref="T"/>/get" before making the requested changes.
	/// </summary>
	public required string? OldState { get; init; }
	
	/// <summary>
	/// The state string that will now be returned by "<see cref="T"/>/get".
	/// </summary>
	public required string NewState { get; init; }
	
	/// <summary>
	/// A map of the creation id to a <see cref="JmapPartial{T}"/> containing any properties of the created
	/// <see cref="T"/> that were not sent by the client.
	///
	/// Null if no <see cref="T"/> objects were successfully created.
	/// </summary>
	public Dictionary<JmapId, JmapPartial<T>>? Created { get; init; }

	/// <summary>
	/// A map of the id of each successfully updated <see cref="T"/> to a <see cref="JmapPartial{T}"/>
	/// containing any property that changed in a way *not* explicitly requested by the PatchObject sent to
	/// the server (e.g. server-set or computed properties), or null if nothing changed beyond what was
	/// requested.
	///
	/// Null if no <see cref="T"/> objects were successfully updated.
	/// </summary>
	public Dictionary<JmapId, JmapPartial<T>?>? Updated { get; init; }

	/// <summary>
	/// The ids of <see cref="T"/> objects that were successfully destroyed, or null if none.
	/// </summary>
	public JmapId[]? Destroyed { get; init; }

	/// <summary>
	/// A map of the creation id to a <see cref="JmapSetError"/> object for each <see cref="T"/> that
	/// failed to be created.
	/// </summary>
	public Dictionary<JmapId, JmapSetError>? NotCreated { get; init; }

	/// <summary>
	/// A map of the <see cref="T"/> id to a <see cref="JmapSetError"/> object for each record that
	/// failed to be updated.
	/// </summary>
	public Dictionary<JmapId, JmapSetError>? NotUpdated { get; init; }

	/// <summary>
	/// A map of the <see cref="T"/> id to a <see cref="JmapSetError"/> object for each record that
	/// failed to be destroyed.
	/// </summary>
	public Dictionary<JmapId, JmapSetError>? NotDestroyed { get; init; }
}
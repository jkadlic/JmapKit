using System.Text.Json;

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
	/// A state string as returned by "<typeparamref name="T"/>/get". If supplied, the string must match the
	/// current state on the server for the request to be processed; otherwise the whole method call is
	/// rejected with a "stateMismatch" error.
	/// </summary>
	public string? IfInState { get; init; }

	/// <summary>
	/// A map of a creation id to <typeparamref name="T"/> objects, or null if no objects are to be created.
	/// </summary>
	public Dictionary<JmapId, T>? Create { get; init; }

	/// <summary>
	/// A map of the id of an existing <typeparamref name="T"/> object to a Patch object describing the changes to apply.
	/// </summary>
	public Dictionary<JmapId, JsonElement>? Update { get; init; }

	/// <summary>
	/// A list of ids for <typeparamref name="T"/> objects to permanently delete.
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
	/// The state string that would have been returned by "<typeparamref name="T"/>/get" before making the requested changes.
	/// </summary>
	public required string? OldState { get; init; }
	
	/// <summary>
	/// The state string that will now be returned by "<typeparamref name="T"/>/get".
	/// </summary>
	public required string NewState { get; init; }
	
	/// <summary>
	/// A map of the creation id to a <see cref="JmapPartial{T}"/> containing any properties of the created
	/// <typeparamref name="T"/> that were not sent by the client.
	///
	/// Null if no <typeparamref name="T"/> objects were successfully created.
	/// </summary>
	public Dictionary<JmapId, JmapPartial<T>>? Created { get; init; }

	/// <summary>
	/// A map of the id of each successfully updated <typeparamref name="T"/> to a <see cref="JmapPartial{T}"/>
	/// containing any property that changed in a way *not* explicitly requested by the PatchObject sent to
	/// the server (e.g. server-set or computed properties), or null if nothing changed beyond what was
	/// requested.
	///
	/// Null if no <typeparamref name="T"/> objects were successfully updated.
	/// </summary>
	public Dictionary<JmapId, JmapPartial<T>?>? Updated { get; init; }

	/// <summary>
	/// The ids of <typeparamref name="T"/> objects that were successfully destroyed, or null if none.
	/// </summary>
	public JmapId[]? Destroyed { get; init; }

	/// <summary>
	/// A map of the creation id to a <see cref="JmapSetError"/> object for each <typeparamref name="T"/> that
	/// failed to be created.
	/// </summary>
	public Dictionary<JmapId, JmapSetError>? NotCreated { get; init; }

	/// <summary>
	/// A map of the <typeparamref name="T"/> id to a <see cref="JmapSetError"/> object for each record that
	/// failed to be updated.
	/// </summary>
	public Dictionary<JmapId, JmapSetError>? NotUpdated { get; init; }

	/// <summary>
	/// A map of the <typeparamref name="T"/> id to a <see cref="JmapSetError"/> object for each record that
	/// failed to be destroyed.
	/// </summary>
	public Dictionary<JmapId, JmapSetError>? NotDestroyed { get; init; }
}
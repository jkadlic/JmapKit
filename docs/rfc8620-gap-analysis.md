# RFC 8620 Gap Analysis

A review of `JmapKit.Core` against [RFC 8620](https://www.rfc-editor.org/rfc/rfc8620),
conducted 2026-09-22 against commit `71c8ed8`.

This is a working document, not a one-time report. Items carry stable ids so they can be
referenced in issues and commits. Filed items track status in their issue; unfiled items
carry a status field here until they are filed.

## How this is triaged

Near enough everything in this document is 1.0 scope. The version number is a claim about
what the library does, and "implements RFC 8620" is not true while §6 (binary data) and §7
(push) are absent — those are core spec, not extensions. A 1.0 shipped without them would
be claiming something it hasn't done.

So scope is not the interesting axis; almost every item is in. What varies is the **kind of
change** each item is, which is what determines ordering and risk:

| Workstream | Kind of change | Cost of doing it late |
|---|---|---|
| **C — Correctness** | Bug fixes behind the existing surface | Blocks everything. Nothing else can be integration-tested while the typed API cannot round-trip with a conformant server. |
| **A — API shape** | Changes the public surface | Breaking. Every one of these alters types callers write against, so late means a major-version bump or a compat shim. |
| **F — Features** | New types and methods | Low risk to existing callers, but each is a chunk of RFC 8620 that 1.0 is claiming to implement. |
| **H — Hardening** | Internal behaviour | Low. Fixable at any point without disturbing callers. |

Within a workstream, order is roughly dependency order. Across workstreams, **C blocks
everything**.

### What can actually wait

Short list, and each needs a reason beyond "it's additive":

- **F-3 (DNS SRV autodiscovery)** — §2.2 makes this explicitly optional for clients: a
  client *"MAY"* use the username domain to attempt autodiscovery. Servers SHOULD publish
  the record; clients need not consume it. A caller can always configure the host directly.
- **F-5 (EventSource)** — a long-lived SSE connection is a different transport shape from
  everything else in the library, and `PushSubscription` (F-4) covers the other half of §7.
  Deferrable on design-cohesion grounds, not because push is optional.
- **H-5 (429 / `Retry-After`)** — resilience, not conformance.
- **H-6 (double env var read)** — cosmetic.

Everything else is 1.0.

**Status legend:** `TODO` · `WIP` · `DONE` · `WONTFIX` (with a note)

---

## C — Correctness (blocks everything)

These are not gaps against optional parts of the spec. They mean the typed API cannot talk
to a conformant JMAP server today.

**Tracked as issues [#7](https://github.com/jkadlic/JmapKit/issues/7)–[#12](https://github.com/jkadlic/JmapKit/issues/12).** Those issues own status; this section
owns the reasoning and the evidence. Items in A, F and H are not filed yet and keep their
status field until they are.

### C-1 · No camelCase naming policy · [#7](https://github.com/jkadlic/JmapKit/issues/7)

**Where:** `JmapKit.Core/JmapClient.cs:43`, and every argument/response record.

`new JsonSerializerOptions()` is created with no naming policy. `JmapSession`,
`JmapRequest` and `JmapResponse` carry `[JsonPropertyName]` attributes; none of
`JmapGetArguments`, `JmapSetArguments`, `JmapChangesArguments`, `JmapCopyArguments`,
`JmapQueryArguments`, `JmapQueryChangesArguments`, `JmapComparator`, `JmapAddedItem`,
`JmapMethodError` or `JmapSetError` do.

Verified by compiling against the library:

```
GET ARGS:   {"AccountId":"u1","Ids":["a1"],"Properties":null}
QUERY ARGS: {"AccountId":"u1","Filter":null,"Sort":[{"Property":"receivedAt",...
```

And in reverse, feeding a spec-correct response into the response record:

```
{"accountId":"u1","state":"s1","list":[],"notFound":[]}
→ JsonException: missing required properties including: 'AccountId', 'State', 'List', 'NotFound'
```

Outbound arguments are rejected by any server; inbound responses cannot be read.

This is currently masked by the test suite: `JmapKit.Core.Tests/TestJmapClient.cs:341` and
the sibling verb tests use PascalCase fixtures (`{"AccountId":"acc1","State":"s1",...}`),
so the bug is asserted as correct behaviour.

**Done when:** the client's `JsonSerializerOptions` sets
`PropertyNamingPolicy = JsonNamingPolicy.CamelCase`, every fixture in
`TestJmapClient.cs` is camelCase, and a round-trip test asserts the exact request body
bytes for at least one verb.

### C-2 · `TryDeserialize` cannot reach the client's serializer options · [#8](https://github.com/jkadlic/JmapKit/issues/8)

**Where:** `JmapKit.Core/JmapMethodResponse.cs`, and the README usage examples.

`TryDeserialize<T>` requires the caller to supply `JsonSerializerOptions`, and the client's
configured instance is private. The README shows `TryDeserialize<...>(new(), ...)` — default
options, which will never match the wire format even after C-1.

Depends on C-1 (the two are one fix in practice: there has to be one shared options
instance, and it has to be reachable).

**Done when:** callers cannot accidentally deserialize with mismatched options — either the
parameter defaults to the client's instance, or the options are exposed on `IJmapClient`
and the README is updated.

### C-3 · DI guards are dead; explicit configuration throws · [#9](https://github.com/jkadlic/JmapKit/issues/9)

**Where:** `JmapKit.Core/JmapKitServiceCollectionExtensions.cs:24,28`.

```csharp
if (!services.Contains(ServiceDescriptor.Singleton(typeof(JmapTokenCredential))))
```

`ServiceDescriptor.Singleton(typeof(X))` binds to the `Singleton<TService>(TService instance)`
overload with `TService = System.Type`, producing a descriptor whose `ServiceType` is
`System.Type` rather than `JmapTokenCredential`. `ServiceDescriptor` also does not override
`Equals`, so `Contains` is reference equality and would be `false` regardless.

Both guards are no-ops. The env-var defaults are always registered, and since
last-registration-wins they shadow whatever the caller registered. Running the README's own
configuration snippet verbatim:

```
Descriptors for JmapClientOptions: 2
THREW: JmapCredentialException: Failed to configure JmapCredential.
       Environment variable 'JMAP_TOKEN' is required...
```

**Fix:** `TryAddSingleton<T>()` from `Microsoft.Extensions.DependencyInjection.Extensions`.

**Done when:** a test registers `JmapClientOptions`/`JmapTokenCredential` explicitly with no
env vars set, calls `AddJmapClient()`, and resolves the caller's instances.

### C-4 · Session discovery requires exactly a 302 · [#10](https://github.com/jkadlic/JmapKit/issues/10)

**Where:** `JmapKit.Core/JmapClient.cs:57`.

Throws unless the well-known endpoint returns `HttpStatusCode.Found`. RFC 8620 §2.2 defines
the session resource as `https://${hostname}[:${port}]/.well-known/jmap` *"(following any
redirects)"* — the redirect is optional. A server that serves the session directly at the
well-known URL (200), or redirects with 301/307/308, fails outright.

**Done when:** 200-at-well-known and each of 301/302/307/308 are covered by tests.

### C-5 · A relative `Location` header crashes with a non-JMAP exception · [#11](https://github.com/jkadlic/JmapKit/issues/11)

**Where:** `JmapKit.Core/JmapClient.cs:66`.

`_sessionEndpoint = location.ToString()` stores the header raw. Relative targets
(`/jmap/session`) are legal and common, and `IHttpClientFactory`'s `HttpClient` has no
`BaseAddress`, so the follow-up request throws:

```
InvalidOperationException: An invalid request URI was provided.
Either the request URI must be an absolute URI or BaseAddress must be set.
```

That escapes the `JmapException` hierarchy the README documents.

**Fix:** resolve against the request URI — `new Uri(requestUri, location)`.

**Done when:** a relative-redirect test passes and no non-`JmapException` can escape
session resolution.

### C-6 · Typed client is transient; session cache never survives, never refreshes · [#12](https://github.com/jkadlic/JmapKit/issues/12)

**Where:** `JmapKit.Core/JmapKitServiceCollectionExtensions.cs:31`, `JmapClient.cs`.

`AddHttpClient<IJmapClient, JmapClient>()` registers a **transient** typed client (verified:
`IJmapClient lifetime: Transient`, distinct instances per resolve). `_sessionEndpoint` and
`_session` are instance fields, so every injection pays two extra round trips before its
first real call.

The opposite problem exists within a single instance: the session is cached forever.
`JmapResponse.SessionState` is never compared against `JmapSession.State`, though RFC 8620
§3.4 provides it precisely so *"clients may use this to detect if this object has changed
and needs to be refetched."* There is no way to force a refresh.

`ResolveSessionAsync` also has a check-then-act race with no locking.

**Note:** the lifetime decision interacts with A-2 (auth refresh) — a singleton session
cache and a refreshable credential want to be designed together. Worth settling both at
once.

**Done when:** session state is shared across resolutions, a `sessionState` change triggers
(or at minimum permits) a refetch, and concurrent `ResolveSessionAsync` calls issue one
request.

---

## A — API shape

Each of these changes the public surface, so late means a breaking change. None of them
block basic use, which is exactly why they are easy to postpone past the point where
postponing is cheap.

### A-1 · Result references · `TODO` · RFC 8620 §3.7

Already acknowledged in the README as in-development. Requires a `ResultReference`
type (`resultOf`, `name`, `path`), `#`-prefixed argument names, and JSON Pointer
[RFC 6901] evaluation including the `*` array-mapping extension.

This is the single largest design item. It changes how arguments are represented
throughout — every argument record needs a way to express "this field comes from call
`c0`" — so it should be settled before the argument records are frozen.

**Done when:** the §3.7 worked example from the RFC round-trips.

### A-2 · Creation-id references · `TODO` · RFC 8620 §3.3, §5.3

A foreign key can reference a record created earlier in the same request via its creation
id prefixed with `#`. This is currently unexpressible: `JmapId.IsValidContent`
(`JmapId.cs`) rejects `#` — correctly, per §1.2 — and nothing replaces it.
`JmapRequest.CreatedIds` exists but there is no way to produce a value that uses it.

Needs a type that is either a real id or a creation-id reference. That type appears in
every user-defined `IJmapObject`, so it must land before consumers write data types
against the surface.

**Done when:** a two-call request that creates a record and references it by `#id` in the
same request serializes correctly.

### A-3 · `Date` / `UTCDate` converters · `TODO` · RFC 8620 §1.4

No converter exists. System.Text.Json's default `DateTimeOffset` output is
`2014-10-30T06:12:00.0000000+00:00`; §1.4 requires `time-secfrac` omitted when zero, any
letters uppercase, and for `UTCDate` a `Z` offset specifically.

Any consumer data type with a timestamp is affected, so this must exist before
`JmapKit.Mail` — or any third-party `IJmapObject` — is written.

**Done when:** round-trip tests cover the §1.4 examples (`2014-10-30T14:12:00+08:00`,
`2014-10-30T06:12:00Z`) and fractional-second suppression.

### A-4 · `FilterOperator` / `FilterCondition` · `TODO` · RFC 8620 §5.5

`JmapQueryArguments.Filter` and `JmapQueryChangesArguments.Filter` are raw `JsonElement`.
No AND/OR/NOT composition. Replacing a `JsonElement` with a typed filter tree later is
breaking.

**Done when:** a nested operator tree composes and serializes per §5.5.

### A-5 · `PatchObject` · `TODO` · RFC 8620 §5.3

`JmapSetArguments.Update` is `Dictionary<JmapId, JsonElement>`. No JSON-Pointer patch
builder and no validation of the `a/b/c` path form. §5.3 notes a whole object is also a
valid `PatchObject`, so the type needs to support both shapes.

**Done when:** a patch builder produces §5.3-conformant pointers and rejects malformed
paths.

### A-6 · Error objects drop spec-permitted properties · `TODO` · RFC 8620 §5.3, §5.4

`JmapSetError` is sealed to `Type`/`Description`. §5.3 states *"Other properties MAY also
be present on the SetError object"*, and the standard errors use them — `invalidProperties`
carries `properties`, and §5.4's `alreadyExists` carries a **required** `existingId`. That
data is silently dropped today. Same for `JmapMethodError`.

Adding an overflow member later is technically additive, but callers will have built
workarounds by then.

**Done when:** unrecognised members survive deserialization and `existingId` is reachable.

### A-7 · Configuration cannot express host+port or a direct session URL · `TODO`

`JmapClientOptions` takes a bare host string. RFC 8620 §2 allows the session URL to be
*"requested directly from the user"*, and §2.2's SRV record supplies a **host and port** —
the port currently has nowhere to go. Widening this later changes the constructor surface.

Pairs naturally with F-3.

**Done when:** a session URL, and a host+port, can both be configured.

---

## F — Features

New types and methods that do not disturb the existing surface. Additive in the
compatibility sense, but these are whole sections of RFC 8620 — §6 and §7 are the core
spec, so 1.0 needs them to mean what it says. F-3 and F-5 are the exceptions; see
[What can actually wait](#what-can-actually-wait).

| Id | Item | RFC | Notes | Status |
|---|---|---|---|---|
| **F-1** | Blob upload / download | §6.1, §6.2 | `UploadUrl`/`DownloadUrl` are parsed onto the session and then unused. Needs RFC 6570 URI Template (level 1) expansion for `{accountId}`, `{blobId}`, `{type}`, `{name}`. 1.0 scope — §6 is core spec. | `TODO` |
| **F-2** | `Blob/copy` | §6.3 | Depends on F-1's blob types. 1.0 scope. | `TODO` |
| **F-3** | DNS SRV autodiscovery | §2.2 | `_jmap._tcp.<domain>`. Blocked on A-7 — the port needs somewhere to go. **Deferrable:** §2.2 makes client-side use explicitly `MAY`. | `TODO` |
| **F-4** | Push: `PushSubscription` `/get` + `/set`, `StateChange` | §7.1, §7.2 | A standard JMAP data type; fits the existing `IJmapObject` machinery once C is done. 1.0 scope — §7 is core spec. | `TODO` |
| **F-5** | Push: EventSource | §7.3 | `EventSourceUrl` is parsed and unused. **Deferrable:** long-lived SSE is a different transport shape from the rest of the library, and F-4 covers the other half of §7. | `TODO` |
| **F-6** | Typed core capability object | §2 | `JmapCoreCapability` holds only the URI string. `maxSizeUpload`, `maxSizeRequest`, `maxConcurrentRequests`, `maxCallsInRequest`, `maxObjectsInGet`, `maxObjectsInSet`, `collationAlgorithms` are reachable only as raw `JsonElement`, and none are enforced client-side. §2 makes these MUST-be-present on the session; a client that ignores them will trip server limits blind. 1.0 scope. | `TODO` |
| **F-7** | `primaryAccounts` convenience | §2 | `AccountId` is `required` on every argument record, but the session already knows the default account per capability. Removes the most common piece of caller boilerplate. Shapes the argument records, so closer to workstream A than it looks — settle alongside A-7. | `TODO` |

---

## H — Hardening (internal, non-breaking)

| Id | Item | Where | Status |
|---|---|---|---|
| **H-1** | Request-level errors are discarded | `JmapClient.cs` throws `JmapProtocolException($"...returned '{StatusCode}'")` and drops the body. RFC 8620 §3.6.1 specifies an RFC 7807 problem-details object distinguishing `unknownCapability`, `notJSON`, `notRequest` and `limit` (the last carrying a required `limit` property naming the limit hit). This is the one error class that says *why* a request was rejected. Cheap, high diagnostic value — worth pulling forward. | `TODO` |
| **H-2** | `SingleOrDefault` on call id | `JmapClient.cs:164` throws `InvalidOperationException` — outside the `JmapException` hierarchy — if a server returns more than one response for a call id. Extensions built on RFC 8620 do exactly that (RFC 8621's `EmailSubmission/set` with `onSuccessUpdateEmail` emits an extra `Email/set` under the same call id). Wants `FirstOrDefault` plus an explicit multi-response accessor. | `TODO` |
| **H-3** | Undisposed response stream | `JmapClient.cs:131` does not dispose the API-path stream; the session path at `:91` correctly uses `await using`. No `HttpResponseMessage` is disposed anywhere. | `TODO` |
| **H-4** | `JmapSession` is uniformly `required` | A server omitting any property — `eventSourceUrl` is the usual casualty — fails the entire session fetch. Stricter than the spec warrants. | `TODO` |
| **H-5** | No 429 / `Retry-After` handling | §8.5 anticipates rate limiting; no retry or backoff exists. | `TODO` |
| **H-6** | Env var read twice | `JmapClientOptions` and `JmapTokenCredential` both call `GetEnvironmentVariable` once for the null check and again for the value, then use `!`. Cosmetic. | `TODO` |

---

## Method

Source read in full at `71c8ed8` (31 files, `JmapKit.Core`). Claims about serialization
output, DI registration behaviour, client lifetime and relative-URI handling were verified
by compiling a probe against the library rather than by inspection. Spec citations are
against the RFC 8620 text, not a summary.

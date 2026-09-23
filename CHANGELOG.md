# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

### Added

- Session discovery, bearer token authentication, and typed JMAP method support
  (`get`, `set`, `changes`, `copy`, `query`, `queryChanges`, `echo`) per RFC 8620.
- `IJmapObject.SupportedMethods`, declaring which JMAP methods a data type supports per
  RFC 8620 §2. `JmapMethodInvocation.Create<T>` validates against it and throws
  `JmapUnsupportedMethodException` for an unsupported method, so an invalid call fails before
  any request is sent.
- `JmapSerializerOptions`, and an `AddJmapClient(configureJson)` overload for adjusting the JSON
  configuration used for all JMAP traffic. The callback adjusts the library's options rather than
  replacing them, so the defaults cannot be lost by accident. Converters claiming
  `JmapMethodInvocation` or `JmapMethodResponse` are rejected with `JmapConfigurationException`,
  since those define the request/response encoding itself. See [JSON handling](docs/json-handling.md).

### Changed

- **Breaking:** `JmapMethodResponse.TryDeserialize<T>` no longer takes a `JsonSerializerOptions`
  parameter. It uses the options the response was read with, so the payload is deserialized exactly
  as the envelope was. Callers passing `new()` previously got a different configuration for the
  payload than the client used for everything else.
- **Breaking:** `JmapClient`'s constructor takes a `JmapSerializerOptions`. Registration via
  `AddJmapClient` is unaffected.

### Fixed

- Property names are now declared with `[JsonPropertyName]` on every type that goes over the wire,
  and the client's serializer applies camelCase as a fallback for types it does not own. Previously
  names were inferred from C# identifiers and serialized as PascalCase, so the typed API could not
  round-trip with a conformant server at all, and `JmapPartial<T>.MergeOnto` silently discarded
  server-side changes rather than failing.
- `JmapComparator.Collation` is omitted when unset rather than sent as `null`. RFC 8620 §5.5 types
  it as `String` with a server-dependent default, not `String|null`.

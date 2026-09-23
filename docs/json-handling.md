# JSON handling

JmapKit builds one `JsonSerializerOptions` instance and uses it for everything it sends and receives,
including the payload you read back through `TryDeserialize<T>`. You configure that instance rather than
supplying your own, so it cannot drift out of sync with the wire format.

## Property names

Declare every serialized property's name with `[JsonPropertyName]`:

```csharp
[JsonPropertyName("totalEmails")] public long TotalEmails { get; init; }
```

JMAP property names are fixed literals defined by the relevant RFC, not a transform of a C# identifier.
[RFC 8620 §1.1](https://www.rfc-editor.org/rfc/rfc8620#section-1.1) says only that they are case
sensitive.

JmapKit's options do apply a camelCase policy as a fallback, and it produces the right answer for most
names. This is intended only as a fallback.

## Custom converters

A type that needs special handling is best served by a `[JsonConverter]` on the type itself. It travels
with the type and needs no configuration anywhere else:

```csharp
[JsonConverter(typeof(MailboxRoleConverter))]
public readonly record struct MailboxRole(string Value);
```

For what can't be expressed on the type pass a callback to `AddJmapClient`:

```csharp
services.AddJmapClient(json =>
{
    json.Converters.Add(new SomeThirdPartyTypeConverter());
    json.NumberHandling = JsonNumberHandling.AllowReadingFromString;
});
```

The callback adjusts JmapKit's options rather than replacing them, so the defaults cannot be lost by
accident. Outside DI, construct a `JmapSerializerOptions` and pass it to the `JmapClient` constructor.

## What you cannot override

Registering a converter for `JmapMethodInvocation` or `JmapMethodResponse` throws
`JmapConfigurationException`. Those two converters encode the positional `[name, arguments, callId]` form
that JMAP requests and responses are made of, and a converter in `Converters` takes precedence over a
type's own `[JsonConverter]`, so registering one would break the wire format rather than customise it.

Everything else is yours to change, including the naming policy. Since JmapKit's own types declare their
names explicitly, changing it affects only your types.

# JmapKit

A .NET client library for [JMAP](https://jmap.io), the JSON Meta Application Protocol
([RFC 8620](https://www.rfc-editor.org/rfc/rfc8620)).

## Status

Pre-1.0. The public API may still change between releases.

**What else could go here?**

#### Implemented
- Session discovery
- Bearer token authentication
- Typed JMAP method support (`get`, `set`, `changes`, `copy`, `query`, `queryChanges`, `echo`)
- JMAP Core data type

#### In Development
- **JmapKit** — Core protocol support: session discovery, authentication, and generic method
  invocation ([RFC 8620](https://www.rfc-editor.org/rfc/rfc8620)).
- Result references, to chain multiple method calls within a single request.

#### Future Development
- **JmapKit.Mail** — Typed support for JMAP Mail ([RFC 8621](https://www.rfc-editor.org/rfc/rfc8621)).


## Install

```
dotnet add package JmapKit
```

## Usage

### Configuration

```csharp
var services = new ServiceCollection();
services.AddJmapClient();

var provider = services.BuildServiceProvider();
var jmap = provider.GetRequiredService<IJmapClient>();
```

`AddJmapClient()` wires up `IJmapClient` behind `HttpClientFactory` and registers default
`JmapClientOptions`/`JmapTokenCredential` instances that read from environment variables, if you
haven't already registered your own:

| Variable     | Purpose                                             |
|--------------|------------------------------------------------------|
| `JMAP_HOST`  | Host to resolve the JMAP session from (no scheme)   |
| `JMAP_TOKEN` | Bearer token sent with every request                |

To configure explicitly instead, register your own instances **before** calling
`AddJmapClient()`:

```csharp
services.AddSingleton(new JmapClientOptions("jmap.fastmail.com"));
services.AddSingleton(new JmapTokenCredential("<token>"));
services.AddJmapClient();
```

The session is resolved lazily on first request. To resolve and inspect it explicitly:

```csharp
var session = await jmap.ResolveSessionAsync(CancellationToken.None);

Console.WriteLine(session.Username);
Console.WriteLine(session.ApiUrl);
Console.WriteLine(session.State);
```

### Defining JMAP data types

Typed method calls (`GetAsync`, `SetAsync`, `QueryAsync`, ...) work against any type that
implements `IJmapObject`, declaring the JMAP object name and the capabilities required to use it:

```csharp
public sealed record Mailbox : IJmapObject
{
    public static string JmapName => "Mailbox";
    public static JmapCapability[] Using => [JmapCoreCapability.Core, new("urn:ietf:params:jmap:mail")];

    public JmapId Id { get; init; }
    public string? Name { get; init; }
    public long TotalEmails { get; init; }
}
```

### Making requests

`Core/echo` needs no data type, so it's a convenient way to check that a call round-trips:

```csharp
var echo = await jmap.EchoAsync(new Dictionary<string, string> { ["hello"] = "world" });
echo.TryDeserialize<Dictionary<string, string>>(new(), out var echoResult, out _);

Console.WriteLine(echoResult!["hello"]); // "world"
```

Typed calls take a typed arguments record and return a `JmapMethodResponse`, deserialized with
`TryDeserialize<T>` into either the matching typed response record or a `JmapMethodError`:

```csharp
var args = new JmapGetArguments<Mailbox> { AccountId = JmapId.Parse("u1234567") };
var response = await jmap.GetAsync(args);

if (response.TryDeserialize<JmapGetResponse<Mailbox>>(new(), out var result, out var error))
{
    foreach (var mailbox in result.List)
        Console.WriteLine($"{mailbox.Name}: {mailbox.TotalEmails} emails");
}
else
{
    Console.WriteLine($"{error.Type}: {error.Description}");
}
```

`SetAsync`, `ChangesAsync`, `CopyAsync`, `QueryAsync`, and `QueryChangesAsync` follow the same
shape. For anything not covered by the typed helpers, build a `JmapRequest` directly and send it
with `InvokeAsync`.

### Error handling

All exceptions thrown by JmapKit derive from `JmapException`:

| Exception                     | Thrown when                                                                 |
|--------------------------------|------------------------------------------------------------------------------|
| `JmapProtocolException`        | The server didn't behave as RFC 8620 expects (bad session response, missing/unexpected method response, unparsable body). |
| `JmapConfigurationException`   | The default `JmapClientOptions()` constructor ran without `JMAP_HOST` set.  |
| `JmapCredentialException`      | The default `JmapTokenCredential()` constructor ran without `JMAP_TOKEN` set. |

Method-level JMAP errors (RFC 8620 §3.6.1) don't throw — they're returned via the `error` out
parameter of `TryDeserialize<T>` as a `JmapMethodError`.

## Contributing

Issues and pull requests are welcome.

## License

[MIT](LICENSE)

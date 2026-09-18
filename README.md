# JmapKit

A .NET client library for [JMAP](https://jmap.io)

#### In Development
- JmapKit.Core - Implementation of the JSON Meta Application Protocol (JMAP) [RFC 8620](https://www.rfc-editor.org/rfc/rfc8620).

#### Future Development
- JmapKit.Mail - Implementation of the JSON Meta Application Protocol (JMAP) for Mail [RFC 8620](https://www.rfc-editor.org/rfc/rfc8620).

## Status

Pre-1.0. The public API may still change between releases.

Currently supports session discovery, bearer token authentication and method invocation/response handling.
Included are typed data structures for JMAP object types.

Not yet implemented:
- Typed helpers for extension capabilities (Mail, Contacts, Calendars, ...).
- Result references to allow multistep operations in a single request.

## Install

Not yet published to NuGet. Until then, reference the project directly:

```xml
<ProjectReference Include="path/to/JmapKit.Core/JmapKit.Core.csproj" />
```

## Quick start

```csharp
using JmapKit;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddJmapClient();

var provider = services.BuildServiceProvider();
var jmap = provider.GetRequiredService<IJmapClient>();

var invocation = new JmapMethodInvocation
{
    Name = "Core/echo",
    Arguments = new Dictionary<string, object?> { { "hello", "world" } },
    CallId = "c0"
};

var request = new JmapRequest
{
    Using = [JmapCoreCapability.Core],
    MethodCalls = [invocation]
};

var response = await jmap.InvokeAsync(request);

Console.WriteLine(response.MethodResponses[0].Arguments["hello"]); // "world"
```

`AddJmapClient()` wires up `IJmapClient` behind `HttpClientFactory` and registers default
`JmapClientOptions`/`JmapTokenCredential` instances that read from environment variables (see
Configuration below) if you haven't already registered your own.

## Configuration

By default, `JmapClientOptions` and `JmapTokenCredential` read from environment variables:

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

> JmapClientOptions & JmapTokenCredential will be registered by default if not already provided.

## Contributing

Issues and pull requests are welcome.

## License

[MIT](LICENSE)
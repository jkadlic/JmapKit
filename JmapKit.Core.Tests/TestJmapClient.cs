using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FluentAssertions;

namespace JmapKit.Tests;

[TestClass]
public class TestJmapClient
{
    private sealed record TestObject : IJmapObject
    {
        public static string JmapName => "TestObject";
        public static JmapCapability[] JmapCapabilities => [new("urn:test:capability"), new("urn:ietf:params:jmap:core")];
        public static JmapMethod[] SupportedMethods =>
        [
            JmapMethod.Get,
            JmapMethod.Set,
            JmapMethod.Changes,
            JmapMethod.Copy,
            JmapMethod.Query,
            JmapMethod.QueryChanges
        ];
    }

    private const string Host = "jmap.example.com";
    private const string SessionUrl = "https://jmap.example.com/session";
    private const string ApiUrl = "https://jmap.example.com/api";

    private const string ValidSessionJson = """
        {
          "username": "user@example.com",
          "apiUrl": "https://jmap.example.com/api",
          "uploadUrl": "https://jmap.example.com/upload/{accountId}/",
          "downloadUrl": "https://jmap.example.com/download/{accountId}/{blobId}/{name}?type={type}",
          "eventSourceUrl": "https://jmap.example.com/event/",
          "state": "state-0",
          "accounts": {},
          "primaryAccounts": {},
          "capabilities": {}
        }
        """;

    private const string ValidResponseJson = """
        {
          "methodResponses": [["Core/echo", {"hello": "world"}, "c0"]],
          "sessionState": "state-0"
        }
        """;

    private static JmapClient CreateClient(FakeHttpMessageHandler handler, Action<JsonSerializerOptions>? configureJson = null) =>
        new(new HttpClient(handler),
            new JmapTokenCredential("test-token"),
            new JmapClientOptions(Host),
            new JmapSerializerOptions(configureJson));

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private static HttpResponseMessage EntrypointRedirect()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Found);
        response.Headers.Location = new Uri(SessionUrl);
        return response;
    }

    private static FakeHttpMessageHandler CreateHappyPathHandler() => new(request =>
        request.RequestUri!.AbsolutePath switch
        {
            "/.well-known/jmap" => EntrypointRedirect(),
            "/session" => JsonResponse(HttpStatusCode.OK, ValidSessionJson),
            "/api" => JsonResponse(HttpStatusCode.OK, ValidResponseJson),
            _ => throw new InvalidOperationException($"Unexpected request to '{request.RequestUri}'.")
        });

    private static FakeHttpMessageHandler CreateApiHandler(Func<string, HttpResponseMessage> apiResponder) => new(request =>
    {
        var path = request.RequestUri!.AbsolutePath;
        if (path == "/.well-known/jmap") return EntrypointRedirect();
        if (path == "/session") return JsonResponse(HttpStatusCode.OK, ValidSessionJson);
        if (path != "/api") throw new InvalidOperationException($"Unexpected request to '{request.RequestUri}'.");

        var body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
        return apiResponder(body);
    });

    private static string BuildResponseJson(string name, string argumentsJson, string callId) =>
        new JsonObject
        {
            ["methodResponses"] = new JsonArray(new JsonArray(name, JsonNode.Parse(argumentsJson), callId)),
            ["sessionState"] = "state-0"
        }.ToJsonString();

    /// <summary>
    /// An /api handler that echoes back whichever method name and call id the client actually sent, paired
    /// with <paramref name="argumentsJson"/> — so a wrong method name sent by the client surfaces as a
    /// mismatch in the test's assertions rather than being silently masked by a canned response.
    /// </summary>
    private static FakeHttpMessageHandler CreateVerbHandler(string argumentsJson) => CreateApiHandler(body =>
    {
        using var doc = JsonDocument.Parse(body);
        var call = doc.RootElement.GetProperty("methodCalls")[0];
        var name = call[0].GetString()!;
        var callId = call[2].GetString()!;
        return JsonResponse(HttpStatusCode.OK, BuildResponseJson(name, argumentsJson, callId));
    });

    private static JmapRequest EchoRequest() => new()
    {
        Using = [JmapCoreCapability.Core],
        MethodCalls = [JmapMethodInvocation.Create<JmapCore>(JmapMethod.Echo, JsonDocument.Parse("{}").RootElement, "c0")]
    };

    [TestMethod]
    public async Task ResolveSessionAsync_EntrypointRedirects_FetchesSessionFromLocation()
    {
        var handler = CreateHappyPathHandler();
        var client = CreateClient(handler);

        var session = await client.ResolveSessionAsync(CancellationToken.None);

        session.Username.Should().Be("user@example.com");
        session.ApiUrl.Should().Be(ApiUrl);
        client.IsEntrypointResolved().Should().BeTrue();
        client.IsSessionResolved().Should().BeTrue();
        handler.Requests.Should().HaveCount(2);
        handler.Requests[0].RequestUri!.AbsolutePath.Should().Be("/.well-known/jmap");
        handler.Requests[1].RequestUri!.Should().Be(new Uri(SessionUrl));
    }

    [TestMethod]
    public async Task ResolveSessionAsync_EntrypointReturnsNon302_ThrowsJmapProtocolException()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = CreateClient(handler);

        var act = () => client.ResolveSessionAsync(CancellationToken.None);

        await act.Should().ThrowAsync<JmapProtocolException>();
    }

    [TestMethod]
    public async Task ResolveSessionAsync_EntrypointMissingLocationHeader_ThrowsJmapProtocolException()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Found));
        var client = CreateClient(handler);

        var act = () => client.ResolveSessionAsync(CancellationToken.None);

        await act.Should().ThrowAsync<JmapProtocolException>();
    }

    [TestMethod]
    public async Task ResolveSessionAsync_SessionEndpointReturnsNonSuccess_ThrowsJmapProtocolException()
    {
        var handler = new FakeHttpMessageHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/.well-known/jmap" => EntrypointRedirect(),
            "/session" => new HttpResponseMessage(HttpStatusCode.InternalServerError),
            _ => throw new InvalidOperationException($"Unexpected request to '{request.RequestUri}'.")
        });
        var client = CreateClient(handler);

        var act = () => client.ResolveSessionAsync(CancellationToken.None);

        await act.Should().ThrowAsync<JmapProtocolException>();
    }

    [TestMethod]
    public async Task ResolveSessionAsync_SessionEndpointReturnsMalformedJson_ThrowsJmapProtocolException()
    {
        var handler = new FakeHttpMessageHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/.well-known/jmap" => EntrypointRedirect(),
            "/session" => JsonResponse(HttpStatusCode.OK, "not json"),
            _ => throw new InvalidOperationException($"Unexpected request to '{request.RequestUri}'.")
        });
        var client = CreateClient(handler);

        var act = () => client.ResolveSessionAsync(CancellationToken.None);

        (await act.Should().ThrowAsync<JmapProtocolException>()).WithInnerException<JsonException>();
    }

    [TestMethod]
    public async Task ResolveSessionAsync_CalledTwice_ReusesCachedSessionWithoutExtraRequests()
    {
        var handler = CreateHappyPathHandler();
        var client = CreateClient(handler);

        await client.ResolveSessionAsync(CancellationToken.None);
        await client.ResolveSessionAsync(CancellationToken.None);

        handler.Requests.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task InvokeAsync_SessionNotYetResolved_ResolvesSessionFirst()
    {
        var handler = CreateHappyPathHandler();
        var client = CreateClient(handler);

        var response = await client.InvokeAsync(EchoRequest(), CancellationToken.None);

        response.SessionState.Should().Be("state-0");
        handler.Requests.Should().HaveCount(3);
        handler.Requests[2].RequestUri!.AbsolutePath.Should().Be("/api");
    }

    [TestMethod]
    public async Task InvokeAsync_ValidResponse_ReturnsDeserializedResponse()
    {
        var handler = CreateHappyPathHandler();
        var client = CreateClient(handler);

        var response = await client.InvokeAsync(EchoRequest(), CancellationToken.None);

        response.MethodResponses.Should().HaveCount(1);
        response.MethodResponses[0].Name.Should().Be("Core/echo");
        response.MethodResponses[0].Arguments.GetProperty("hello").GetString().Should().Be("world");
    }

    [TestMethod]
    public async Task InvokeAsync_ApiEndpointReturnsNonSuccess_ThrowsJmapProtocolException()
    {
        var handler = new FakeHttpMessageHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/.well-known/jmap" => EntrypointRedirect(),
            "/session" => JsonResponse(HttpStatusCode.OK, ValidSessionJson),
            "/api" => new HttpResponseMessage(HttpStatusCode.BadRequest),
            _ => throw new InvalidOperationException($"Unexpected request to '{request.RequestUri}'.")
        });
        var client = CreateClient(handler);

        var act = () => client.InvokeAsync(EchoRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<JmapProtocolException>();
    }

    [TestMethod]
    public async Task InvokeAsync_ApiEndpointReturnsMalformedJson_ThrowsJmapProtocolException()
    {
        var handler = new FakeHttpMessageHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/.well-known/jmap" => EntrypointRedirect(),
            "/session" => JsonResponse(HttpStatusCode.OK, ValidSessionJson),
            "/api" => JsonResponse(HttpStatusCode.OK, "not json"),
            _ => throw new InvalidOperationException($"Unexpected request to '{request.RequestUri}'.")
        });
        var client = CreateClient(handler);

        var act = () => client.InvokeAsync(EchoRequest(), CancellationToken.None);

        (await act.Should().ThrowAsync<JmapProtocolException>()).WithInnerException<JsonException>();
    }

    [TestMethod]
    public async Task InvokeAsync_ApiEndpointReturnsNullJson_ThrowsJmapProtocolException()
    {
        var handler = new FakeHttpMessageHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/.well-known/jmap" => EntrypointRedirect(),
            "/session" => JsonResponse(HttpStatusCode.OK, ValidSessionJson),
            "/api" => JsonResponse(HttpStatusCode.OK, "null"),
            _ => throw new InvalidOperationException($"Unexpected request to '{request.RequestUri}'.")
        });
        var client = CreateClient(handler);

        var act = () => client.InvokeAsync(EchoRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<JmapProtocolException>();
    }

    [TestMethod]
    public async Task InvokeAsync_SendsBearerTokenHeaderOnEveryRequest()
    {
        var handler = CreateHappyPathHandler();
        var client = CreateClient(handler);

        await client.InvokeAsync(EchoRequest(), CancellationToken.None);

        handler.Requests.Should().OnlyContain(r => r.Headers.Authorization!.ToString() == "Bearer test-token");
    }

    [TestMethod]
    public void IsEntrypointResolved_BeforeAnyCall_ReturnsFalse()
    {
        var client = CreateClient(CreateHappyPathHandler());

        client.IsEntrypointResolved().Should().BeFalse();
    }

    [TestMethod]
    public void IsSessionResolved_BeforeAnyCall_ReturnsFalse()
    {
        var client = CreateClient(CreateHappyPathHandler());

        client.IsSessionResolved().Should().BeFalse();
    }

    // ----- Echo -----

    [TestMethod]
    public async Task EchoAsync_ValidResponse_ReturnsDeserializableMethodResponse()
    {
        var handler = CreateVerbHandler("""{"hello":"world"}""");
        var client = CreateClient(handler);

        var response = await client.EchoAsync(new Dictionary<string, string> { ["hello"] = "world" }, CancellationToken.None);

        response.Name.Should().Be("Core/echo");
        response.IsError.Should().BeFalse();
        response.TryDeserialize<Dictionary<string, string>>(out var value, out var error)
            .Should().BeTrue();
        value.Should().ContainKey("hello").WhoseValue.Should().Be("world");
        error.Should().BeNull();
    }

    [TestMethod]
    public async Task EchoAsync_RequestShape_UsesOnlyCoreCapability()
    {
        var handler = CreateVerbHandler("""{"hello":"world"}""");
        var client = CreateClient(handler);

        await client.EchoAsync(new Dictionary<string, string> { ["hello"] = "world" }, CancellationToken.None);

        var body = await handler.Requests[2].Content!.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("using").EnumerateArray().Select(e => e.GetString())
            .Should().BeEquivalentTo(["urn:ietf:params:jmap:core"]);
        var call = doc.RootElement.GetProperty("methodCalls")[0];
        call[0].GetString().Should().Be("Core/echo");
        call[2].GetString().Should().Be("c0");
    }

    // ----- Serializer configuration -----

    /// <summary>
    /// A value object with no built-in JSON representation, standing in for the realistic reason to
    /// configure the serializer: the caller's own type needs a converter.
    /// </summary>
    private readonly record struct Tag(string Value);

    private sealed class TagConverter : JsonConverter<Tag>
    {
        public override Tag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString()!.ToUpperInvariant());

        public override void Write(Utf8JsonWriter writer, Tag value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Value.ToLowerInvariant());
    }

    private sealed record TaggedObject : IJmapObject
    {
        public static string JmapName => "TaggedObject";
        public static JmapCapability[] JmapCapabilities => [new("urn:ietf:params:jmap:core")];
        public static JmapMethod[] SupportedMethods => [JmapMethod.Get, JmapMethod.Set];

        [JsonPropertyName("tag")] public Tag Tag { get; init; }
    }

    /// <summary>
    /// The converter registered through <c>configureJson</c> has to reach the response payload, not just the
    /// envelope — the payload is deserialized by <see cref="JmapMethodResponse.TryDeserialize{T}"/>, which
    /// used to take its options from the caller rather than from the client.
    /// </summary>
    [TestMethod]
    public async Task TryDeserialize_ConfiguredConverter_IsAppliedToTheResponsePayload()
    {
        var handler = CreateVerbHandler(
            """{"accountId":"acc1","state":"s1","list":[{"tag":"inbox"}],"notFound":[]}""");
        var client = CreateClient(handler, json => json.Converters.Add(new TagConverter()));

        var response = await client.GetAsync(
            new JmapGetArguments<TaggedObject> { AccountId = JmapId.Parse("acc1") }, CancellationToken.None);

        response.TryDeserialize<JmapGetResponse<TaggedObject>>(out var value, out var error)
            .Should().BeTrue();
        error.Should().BeNull();
        value!.List.Should().ContainSingle()
            .Which.Tag.Value.Should().Be("INBOX", "the caller's converter should have read the payload");
    }

    [TestMethod]
    public async Task InvokeAsync_ConfiguredConverter_IsAppliedToOutgoingArguments()
    {
        var handler = CreateVerbHandler("""{"accountId":"acc1","state":"s1","list":[],"notFound":[]}""");
        var client = CreateClient(handler, json => json.Converters.Add(new TagConverter()));

        await client.SetAsync(
            new JmapSetArguments<TaggedObject>
            {
                AccountId = JmapId.Parse("acc1"),
                Create = new() { [JmapId.Parse("k1")] = new TaggedObject { Tag = new Tag("INBOX") } },
            },
            CancellationToken.None);

        var body = await handler.Requests[2].Content!.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("methodCalls")[0][1]
            .GetProperty("create").GetProperty("k1").GetProperty("tag").GetString()
            .Should().Be("inbox");
    }

    // ----- Typed verb methods -----

    [TestMethod]
    public async Task GetAsync_ValidResponse_ReturnsDeserializableMethodResponse()
    {
        var handler = CreateVerbHandler("""{"accountId":"acc1","state":"s1","list":[],"notFound":[]}""");
        var client = CreateClient(handler);

        var response = await client.GetAsync(new JmapGetArguments<TestObject> { AccountId = JmapId.Parse("acc1") }, CancellationToken.None);

        response.Name.Should().Be("TestObject/get");
        response.IsError.Should().BeFalse();
        response.TryDeserialize<JmapGetResponse<TestObject>>(out var value, out var error)
            .Should().BeTrue();
        value!.AccountId.Should().Be(JmapId.Parse("acc1"));
        error.Should().BeNull();
    }

    [TestMethod]
    public async Task GetAsync_RequestShape_IncludesCoreAndTypeCapabilities()
    {
        var handler = CreateVerbHandler("""{"accountId":"acc1","state":"s1","list":[],"notFound":[]}""");
        var client = CreateClient(handler);

        await client.GetAsync(new JmapGetArguments<TestObject> { AccountId = JmapId.Parse("acc1") }, CancellationToken.None);

        var body = await handler.Requests[2].Content!.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("using").EnumerateArray().Select(e => e.GetString())
            .Should().BeEquivalentTo(["urn:ietf:params:jmap:core", "urn:test:capability"]);
        var call = doc.RootElement.GetProperty("methodCalls")[0];
        call[0].GetString().Should().Be("TestObject/get");
        call[2].GetString().Should().Be("c0");
    }

    /// <summary>
    /// Asserts the exact argument names the client puts on the wire. RFC 8620 §1.1 makes property names case
    /// sensitive, so a mis-cased name is an unrecognised argument rather than a variant spelling.
    /// </summary>
    /// <remarks>
    /// Deliberately not written against "Core/echo": RFC 8620 §4 has the server return echo's arguments
    /// exactly as given, and those arguments are opaque, so PascalCase round-trips through echo perfectly and
    /// would not detect this at all.
    /// </remarks>
    [TestMethod]
    public async Task GetAsync_RequestShape_SerializesArgumentsWithSpecPropertyNames()
    {
        var handler = CreateVerbHandler("""{"accountId":"acc1","state":"s1","list":[],"notFound":[]}""");
        var client = CreateClient(handler);

        await client.GetAsync(
            new JmapGetArguments<TestObject>
            {
                AccountId = JmapId.Parse("acc1"),
                Ids = [JmapId.Parse("id1")],
                Properties = ["name"]
            },
            CancellationToken.None);

        var body = await handler.Requests[2].Content!.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var arguments = doc.RootElement.GetProperty("methodCalls")[0][1];

        arguments.GetRawText().Should().Be("""{"accountId":"acc1","ids":["id1"],"properties":["name"]}""");
    }

    [TestMethod]
    public async Task QueryAsync_RequestShape_SerializesNestedComparatorWithSpecPropertyNames()
    {
        var handler = CreateVerbHandler(
            """{"accountId":"acc1","queryState":"qs1","canCalculateChanges":false,"position":0,"ids":[]}""");
        var client = CreateClient(handler);

        await client.QueryAsync(
            new JmapQueryArguments<TestObject>
            {
                AccountId = JmapId.Parse("acc1"),
                Sort = [new JmapComparator { Property = "name", IsAscending = false }]
            },
            CancellationToken.None);

        var body = await handler.Requests[2].Content!.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var sort = doc.RootElement.GetProperty("methodCalls")[0][1].GetProperty("sort")[0];

        // No "collation": it is typed "String" with a server-dependent default rather than "String|null"
        // (RFC 8620 §5.5), so an unset collation is omitted rather than sent as null.
        sort.GetRawText().Should().Be("""{"property":"name","isAscending":false}""");
    }

    [TestMethod]
    public async Task GetAsync_ServerReturnsMethodError_ReturnsMethodResponseWithoutThrowing()
    {
        var handler = CreateApiHandler(_ =>
            JsonResponse(HttpStatusCode.OK, BuildResponseJson("error", """{"type":"accountNotFound"}""", "c0")));
        var client = CreateClient(handler);

        var response = await client.GetAsync(new JmapGetArguments<TestObject> { AccountId = JmapId.Parse("acc1") }, CancellationToken.None);

        response.IsError.Should().BeTrue();
        response.TryDeserialize<JmapGetResponse<TestObject>>(out var value, out var error)
            .Should().BeFalse();
        value.Should().BeNull();
        error!.Type.Should().Be("accountNotFound");
    }

    [TestMethod]
    public async Task GetAsync_NoResponseForCallId_ThrowsJmapProtocolException()
    {
        var handler = CreateApiHandler(_ =>
            JsonResponse(HttpStatusCode.OK, BuildResponseJson("TestObject/get", "{}", "wrong-call-id")));
        var client = CreateClient(handler);

        var act = () => client.GetAsync(new JmapGetArguments<TestObject> { AccountId = JmapId.Parse("acc1") }, CancellationToken.None);

        await act.Should().ThrowAsync<JmapProtocolException>();
    }

    [TestMethod]
    public async Task GetAsync_UnexpectedMethodName_ThrowsJmapProtocolException()
    {
        var handler = CreateApiHandler(_ =>
            JsonResponse(HttpStatusCode.OK, BuildResponseJson("SomethingElse/get", "{}", "c0")));
        var client = CreateClient(handler);

        var act = () => client.GetAsync(new JmapGetArguments<TestObject> { AccountId = JmapId.Parse("acc1") }, CancellationToken.None);

        await act.Should().ThrowAsync<JmapProtocolException>();
    }

    // ----- Typed verb methods -----

    [TestMethod]
    public async Task SetAsync_ValidResponse_ReturnsDeserializableMethodResponse()
    {
        var handler = CreateVerbHandler("""{"accountId":"acc1","oldState":null,"newState":"s2"}""");
        var client = CreateClient(handler);

        var response = await client.SetAsync(new JmapSetArguments<TestObject> { AccountId = JmapId.Parse("acc1") }, CancellationToken.None);

        response.Name.Should().Be("TestObject/set");
        response.TryDeserialize<JmapSetResponse<TestObject>>(out var value, out _)
            .Should().BeTrue();
        value!.NewState.Should().Be("s2");
    }

    [TestMethod]
    public async Task ChangesAsync_ValidResponse_ReturnsDeserializableMethodResponse()
    {
        var handler = CreateVerbHandler(
            """{"accountId":"acc1","oldState":"s1","newState":"s2","hasMoreChanges":false,"created":[],"updated":[],"destroyed":[]}""");
        var client = CreateClient(handler);

        var response = await client.ChangesAsync(
            new JmapChangesArguments<TestObject> { AccountId = JmapId.Parse("acc1"), SinceState = "s1" },
            CancellationToken.None);

        response.Name.Should().Be("TestObject/changes");
        response.TryDeserialize<JmapChangesResponse<TestObject>>(out var value, out _)
            .Should().BeTrue();
        value!.HasMoreChanges.Should().BeFalse();
    }

    [TestMethod]
    public async Task CopyAsync_ValidResponse_ReturnsDeserializableMethodResponse()
    {
        var handler = CreateVerbHandler("""{"fromAccountId":"acc1","accountId":"acc2","oldState":null,"newState":"s2"}""");
        var client = CreateClient(handler);

        var response = await client.CopyAsync(
            new JmapCopyArguments<TestObject>
            {
                FromAccountId = JmapId.Parse("acc1"),
                AccountId = JmapId.Parse("acc2"),
                Create = new Dictionary<JmapId, TestObject>()
            },
            CancellationToken.None);

        response.Name.Should().Be("TestObject/copy");
        response.TryDeserialize<JmapCopyResponse<TestObject>>(out var value, out _)
            .Should().BeTrue();
        value!.NewState.Should().Be("s2");
    }

    [TestMethod]
    public async Task QueryAsync_ValidResponse_ReturnsDeserializableMethodResponse()
    {
        var handler = CreateVerbHandler(
            """{"accountId":"acc1","queryState":"qs1","canCalculateChanges":false,"position":0,"ids":[]}""");
        var client = CreateClient(handler);

        var response = await client.QueryAsync(new JmapQueryArguments<TestObject> { AccountId = JmapId.Parse("acc1") }, CancellationToken.None);

        response.Name.Should().Be("TestObject/query");
        response.TryDeserialize<JmapQueryResponse<TestObject>>(out var value, out _)
            .Should().BeTrue();
        value!.QueryState.Should().Be("qs1");
    }

    [TestMethod]
    public async Task QueryChangesAsync_ValidResponse_ReturnsDeserializableMethodResponse()
    {
        var handler = CreateVerbHandler(
            """{"accountId":"acc1","oldQueryState":"qs1","newQueryState":"qs2","removed":[],"added":[]}""");
        var client = CreateClient(handler);

        var response = await client.QueryChangesAsync(
            new JmapQueryChangesArguments<TestObject> { AccountId = JmapId.Parse("acc1"), SinceQueryState = "qs1" },
            CancellationToken.None);

        response.Name.Should().Be("TestObject/queryChanges");
        response.TryDeserialize<JmapQueryChangesResponse<TestObject>>(out var value, out _)
            .Should().BeTrue();
        value!.NewQueryState.Should().Be("qs2");
    }

    // ----- Method enforcement -----

    private static FakeHttpMessageHandler CreateNoRequestsExpectedHandler() =>
        new(request => throw new InvalidOperationException($"Unexpected request to '{request.RequestUri}'."));

    private static IEnumerable<object[]> UnsupportedJmapCoreCalls()
    {
        Task Get(JmapClient client) => client.GetAsync(new JmapGetArguments<JmapCore> { AccountId = JmapId.Parse("acc1") });
        Task Set(JmapClient client) => client.SetAsync(new JmapSetArguments<JmapCore> { AccountId = JmapId.Parse("acc1") });
        Task Changes(JmapClient client) => client.ChangesAsync(
            new JmapChangesArguments<JmapCore> { AccountId = JmapId.Parse("acc1"), SinceState = "s1" });
        Task Copy(JmapClient client) => client.CopyAsync(new JmapCopyArguments<JmapCore>
        {
            AccountId = JmapId.Parse("acc1"),
            FromAccountId = JmapId.Parse("acc1"),
            Create = new Dictionary<JmapId, JmapCore>()
        });
        Task Query(JmapClient client) => client.QueryAsync(new JmapQueryArguments<JmapCore> { AccountId = JmapId.Parse("acc1") });
        Task QueryChanges(JmapClient client) => client.QueryChangesAsync(
            new JmapQueryChangesArguments<JmapCore> { AccountId = JmapId.Parse("acc1"), SinceQueryState = "qs1" });

        yield return [nameof(Get), (Func<JmapClient, Task>)Get];
        yield return [nameof(Set), (Func<JmapClient, Task>)Set];
        yield return [nameof(Changes), (Func<JmapClient, Task>)Changes];
        yield return [nameof(Copy), (Func<JmapClient, Task>)Copy];
        yield return [nameof(Query), (Func<JmapClient, Task>)Query];
        yield return [nameof(QueryChanges), (Func<JmapClient, Task>)QueryChanges];
    }

    [TestMethod]
    [DynamicData(nameof(UnsupportedJmapCoreCalls))]
    public async Task VerbAsync_MethodNotSupportedByObject_ThrowsWithoutSendingRequest(string _, Func<JmapClient, Task> invoke)
    {
        var client = CreateClient(CreateNoRequestsExpectedHandler());

        var act = () => invoke(client);

        await act.Should().ThrowAsync<JmapUnsupportedMethodException>();
    }
}

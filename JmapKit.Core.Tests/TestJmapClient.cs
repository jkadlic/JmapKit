using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;

namespace JmapKit.Tests;

[TestClass]
public class TestJmapClient
{
    private sealed record TestObject : IJmapObject
    {
        public static string JmapName => "TestObject";
        public static JmapCapability Capability => new("urn:test:capability");
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

    private static JmapClient CreateClient(FakeHttpMessageHandler handler) =>
        new(new HttpClient(handler), new JmapTokenCredential("test-token"), new JmapClientOptions(Host));

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
        MethodCalls = [new JmapMethodInvocation { Name = "Core/echo", Arguments = JsonDocument.Parse("{}").RootElement, CallId = "c0" }]
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

    // ----- Typed verb methods -----

    [TestMethod]
    public async Task GetAsync_ValidResponse_ReturnsDeserializableMethodResponse()
    {
        var handler = CreateVerbHandler("""{"AccountId":"acc1","State":"s1","List":[],"NotFound":[]}""");
        var client = CreateClient(handler);

        var response = await client.GetAsync(new JmapGetArguments<TestObject> { AccountId = JmapId.Parse("acc1") }, CancellationToken.None);

        response.Name.Should().Be("TestObject/get");
        response.IsError.Should().BeFalse();
        response.TryDeserialize<JmapGetResponse<TestObject>>(new JsonSerializerOptions(), out var value, out var error)
            .Should().BeTrue();
        value!.AccountId.Should().Be(JmapId.Parse("acc1"));
        error.Should().BeNull();
    }

    [TestMethod]
    public async Task GetAsync_RequestShape_IncludesCoreAndTypeCapabilities()
    {
        var handler = CreateVerbHandler("""{"AccountId":"acc1","State":"s1","List":[],"NotFound":[]}""");
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

    [TestMethod]
    public async Task GetAsync_ServerReturnsMethodError_ReturnsMethodResponseWithoutThrowing()
    {
        var handler = CreateApiHandler(_ =>
            JsonResponse(HttpStatusCode.OK, BuildResponseJson("error", """{"Type":"accountNotFound"}""", "c0")));
        var client = CreateClient(handler);

        var response = await client.GetAsync(new JmapGetArguments<TestObject> { AccountId = JmapId.Parse("acc1") }, CancellationToken.None);

        response.IsError.Should().BeTrue();
        response.TryDeserialize<JmapGetResponse<TestObject>>(new JsonSerializerOptions(), out var value, out var error)
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
        var handler = CreateVerbHandler("""{"AccountId":"acc1","OldState":null,"NewState":"s2"}""");
        var client = CreateClient(handler);

        var response = await client.SetAsync(new JmapSetArguments<TestObject> { AccountId = JmapId.Parse("acc1") }, CancellationToken.None);

        response.Name.Should().Be("TestObject/set");
        response.TryDeserialize<JmapSetResponse<TestObject>>(new JsonSerializerOptions(), out var value, out _)
            .Should().BeTrue();
        value!.NewState.Should().Be("s2");
    }

    [TestMethod]
    public async Task ChangesAsync_ValidResponse_ReturnsDeserializableMethodResponse()
    {
        var handler = CreateVerbHandler(
            """{"AccountId":"acc1","OldState":"s1","NewState":"s2","HasMoreChanges":false,"Created":[],"Updated":[],"Destroyed":[]}""");
        var client = CreateClient(handler);

        var response = await client.ChangesAsync(
            new JmapChangesArguments<TestObject> { AccountId = JmapId.Parse("acc1"), SinceState = "s1" },
            CancellationToken.None);

        response.Name.Should().Be("TestObject/changes");
        response.TryDeserialize<JmapChangesResponse<TestObject>>(new JsonSerializerOptions(), out var value, out _)
            .Should().BeTrue();
        value!.HasMoreChanges.Should().BeFalse();
    }

    [TestMethod]
    public async Task CopyAsync_ValidResponse_ReturnsDeserializableMethodResponse()
    {
        var handler = CreateVerbHandler("""{"FromAccountId":"acc1","AccountId":"acc2","OldState":null,"NewState":"s2"}""");
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
        response.TryDeserialize<JmapCopyResponse<TestObject>>(new JsonSerializerOptions(), out var value, out _)
            .Should().BeTrue();
        value!.NewState.Should().Be("s2");
    }

    [TestMethod]
    public async Task QueryAsync_ValidResponse_ReturnsDeserializableMethodResponse()
    {
        var handler = CreateVerbHandler(
            """{"AccountId":"acc1","QueryState":"qs1","CanCalculateChanges":false,"Position":0,"Ids":[]}""");
        var client = CreateClient(handler);

        var response = await client.QueryAsync(new JmapQueryArguments<TestObject> { AccountId = JmapId.Parse("acc1") }, CancellationToken.None);

        response.Name.Should().Be("TestObject/query");
        response.TryDeserialize<JmapQueryResponse<TestObject>>(new JsonSerializerOptions(), out var value, out _)
            .Should().BeTrue();
        value!.QueryState.Should().Be("qs1");
    }

    [TestMethod]
    public async Task QueryChangesAsync_ValidResponse_ReturnsDeserializableMethodResponse()
    {
        var handler = CreateVerbHandler(
            """{"AccountId":"acc1","OldQueryState":"qs1","NewQueryState":"qs2","Removed":[],"Added":[]}""");
        var client = CreateClient(handler);

        var response = await client.QueryChangesAsync(
            new JmapQueryChangesArguments<TestObject> { AccountId = JmapId.Parse("acc1"), SinceQueryState = "qs1" },
            CancellationToken.None);

        response.Name.Should().Be("TestObject/queryChanges");
        response.TryDeserialize<JmapQueryChangesResponse<TestObject>>(new JsonSerializerOptions(), out var value, out _)
            .Should().BeTrue();
        value!.NewQueryState.Should().Be("qs2");
    }
}
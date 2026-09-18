using System.Text.Json;
using FluentAssertions;

namespace JmapKit.Tests;

[TestClass]
public class TestJmapResponseConverter
{
    private static JsonSerializerOptions Options { get; } = new();

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        Options.Converters.Add(new JmapObjectConverter());
    }

    [TestMethod]
    public void Deserialize_ValidResponse_ReturnsResponseObject()
    {
        var json = """
        {
          "methodResponses": [
            [ "method1", { "arg1": 3, "arg2": "foo" }, "c1" ]
          ],
          "createdIds": { "clientId1": "serverId1" },
          "sessionState": "abc123"
        }
        """;

        var r = JsonSerializer.Deserialize<JmapResponse>(json, Options);

        r.Should().NotBeNull();
        r.SessionState.Should().Be("abc123");
        r.CreatedIds.Should().NotBeNull();
        r.CreatedIds["clientId1"].Should().Be((JmapId)"serverId1");
        r.MethodResponses.Should().HaveCount(1);
        r.MethodResponses[0].Name.Should().Be("method1");
        r.MethodResponses[0].CallId.Should().Be("c1");
        r.MethodResponses[0].Arguments["arg1"].Should().Be(3);
        r.MethodResponses[0].Arguments["arg2"].Should().Be("foo");
    }

    [TestMethod]
    public void Deserialize_WithoutCreatedIds_ReturnsNullCreatedIds()
    {
        var json = """
        {
          "methodResponses": [],
          "sessionState": "abc123"
        }
        """;

        var r = JsonSerializer.Deserialize<JmapResponse>(json, Options);

        r.Should().NotBeNull();
        r.CreatedIds.Should().BeNull();
    }

    [TestMethod]
    public void Deserialize_UnknownProperty_IsIgnored()
    {
        var json = """
        {
          "methodResponses": [],
          "sessionState": "abc123",
          "somethingNew": { "a": 1 }
        }
        """;

        var r = JsonSerializer.Deserialize<JmapResponse>(json, Options);

        r.Should().NotBeNull();
        r.SessionState.Should().Be("abc123");
    }

    [TestMethod]
    public void Deserialize_MissingMethodResponses_ThrowsJsonException()
    {
        var json = """{ "sessionState": "abc123" }""";

        var act = () => JsonSerializer.Deserialize<JmapResponse>(json, Options);

        act.Should().Throw<JsonException>();
    }

    [TestMethod]
    public void Deserialize_MissingSessionState_ThrowsJsonException()
    {
        var json = """{ "methodResponses": [] }""";

        var act = () => JsonSerializer.Deserialize<JmapResponse>(json, Options);

        act.Should().Throw<JsonException>();
    }
}
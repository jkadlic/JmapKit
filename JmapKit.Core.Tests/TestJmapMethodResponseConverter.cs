using System.Text.Json;
using FluentAssertions;

namespace JmapKit.Tests;

[TestClass]
public sealed class TestJmapMethodResponseConverter
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
        [ "method1", {
          "arg1": 3,
          "arg2": "foo"
        }, "c1" ]
        """;

        var r = JsonSerializer.Deserialize<JmapMethodResponse>(json, Options);

        r.Should().NotBeNull();
        r.Name.Should().Be("method1");
        r.CallId.Should().Be("c1");
        r.Arguments["arg1"].Should().Be(3);
        r.Arguments["arg2"].Should().Be("foo");
    }

    [TestMethod]
    public void Deserialize_NestedObjectAndArrayArguments_ReturnsPlainClrValues()
    {
        var json = """
        [ "method1", {
          "obj": { "nested": true },
          "list": [1, "two", null]
        }, "c1" ]
        """;

        var r = JsonSerializer.Deserialize<JmapMethodResponse>(json, Options);

        r.Should().NotBeNull();
        var obj = r.Arguments["obj"].Should().BeOfType<Dictionary<string, object?>>().Subject;
        obj["nested"].Should().Be(true);

        var list = r.Arguments["list"].Should().BeOfType<List<object?>>().Subject;
        list.Should().BeEquivalentTo(new object?[] { 1L, "two", null });
    }

    [TestMethod]
    public void Deserialize_BooleanAndNullArguments_ReturnsExpectedTypes()
    {
        var json = """["method1", { "a": true, "b": false, "c": null }, "c1"]""";

        var r = JsonSerializer.Deserialize<JmapMethodResponse>(json, Options);

        r.Should().NotBeNull();
        r.Arguments["a"].Should().Be(true);
        r.Arguments["b"].Should().Be(false);
        r.Arguments["c"].Should().BeNull();
    }

    [TestMethod]
    public void Deserialize_NotAnArray_ThrowsJsonException()
    {
        var act = () => JsonSerializer.Deserialize<JmapMethodResponse>("""{"name":"method1"}""", Options);

        act.Should().Throw<JsonException>();
    }

    [TestMethod]
    public void Deserialize_MissingCallId_ThrowsJsonException()
    {
        var act = () => JsonSerializer.Deserialize<JmapMethodResponse>("""["method1", {}]""", Options);

        act.Should().Throw<JsonException>();
    }

    [TestMethod]
    public void Serialize_ThrowsNotSupportedException()
    {
        var response = new JmapMethodResponse
        {
            Name = "method1",
            Arguments = new Dictionary<string, object?>(),
            CallId = "c1"
        };

        var act = () => JsonSerializer.Serialize(response, Options);

        act.Should().Throw<NotSupportedException>();
    }
}

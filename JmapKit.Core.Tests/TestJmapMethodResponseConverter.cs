using System.Text.Json;
using FluentAssertions;

namespace JmapKit.Tests;

[TestClass]
public sealed class TestJmapMethodResponseConverter
{
    private static JsonSerializerOptions Options { get; } = new();

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
        r.Arguments.GetProperty("arg1").GetInt32().Should().Be(3);
        r.Arguments.GetProperty("arg2").GetString().Should().Be("foo");
    }

    [TestMethod]
    public void Deserialize_NestedObjectAndArrayArguments_PreservesRawJson()
    {
        var json = """
        [ "method1", {
          "obj": { "nested": true },
          "list": [1, "two", null]
        }, "c1" ]
        """;

        var r = JsonSerializer.Deserialize<JmapMethodResponse>(json, Options);

        r.Should().NotBeNull();
        r.Arguments.GetProperty("obj").GetProperty("nested").GetBoolean().Should().BeTrue();

        var list = r.Arguments.GetProperty("list");
        list.ValueKind.Should().Be(JsonValueKind.Array);
        list[0].GetInt32().Should().Be(1);
        list[1].GetString().Should().Be("two");
        list[2].ValueKind.Should().Be(JsonValueKind.Null);
    }

    [TestMethod]
    public void Deserialize_BooleanAndNullArguments_ReturnsExpectedTypes()
    {
        var json = """["method1", { "a": true, "b": false, "c": null }, "c1"]""";

        var r = JsonSerializer.Deserialize<JmapMethodResponse>(json, Options);

        r.Should().NotBeNull();
        r.Arguments.GetProperty("a").GetBoolean().Should().BeTrue();
        r.Arguments.GetProperty("b").GetBoolean().Should().BeFalse();
        r.Arguments.GetProperty("c").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [TestMethod]
    public void Deserialize_NotAnArray_ThrowsJsonException()
    {
        var act = () => JsonSerializer.Deserialize<JmapMethodResponse>("""{"name":"method1"}""", Options);

        act.Should().Throw<JsonException>();
    }

    [TestMethod]
    public void Deserialize_ArgumentsNotAnObject_ThrowsJsonException()
    {
        var act = () => JsonSerializer.Deserialize<JmapMethodResponse>("""["method1", [], "c1"]""", Options);

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
            Arguments = JsonDocument.Parse("{}").RootElement,
            CallId = "c1"
        };

        var act = () => JsonSerializer.Serialize(response, Options);

        act.Should().Throw<NotSupportedException>();
    }
}
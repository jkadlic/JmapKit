using System.Text.Json;
using FluentAssertions;

namespace JmapKit.Tests;

[TestClass]
public class TestJmapMethodInvocationConverter
{
    [TestMethod]
    public void Serialize_ValidInvocation_ReturnsInvocationString()
    {
        var invocation = JmapMethodInvocation.Create<JmapCore>(
            JmapMethod.Echo,
            JsonSerializer.SerializeToElement(new Dictionary<string, object?>
            {
                { "arg1", 3 },
                { "arg2", "foo" },
            }),
            "c0");

        var r = JsonSerializer.Serialize(invocation);

        r.Should().Be("""["Core/echo",{"arg1":3,"arg2":"foo"},"c0"]""");
    }

    [TestMethod]
    public void Serialize_EmptyArguments_WritesEmptyObject()
    {
        var invocation = JmapMethodInvocation.Create<JmapCore>(
            JmapMethod.Echo,
            JsonDocument.Parse("{}").RootElement,
            "c0");

        var r = JsonSerializer.Serialize(invocation);

        r.Should().Be("""["Core/echo",{},"c0"]""");
    }

    [TestMethod]
    public void Serialize_NullArgumentValue_WritesJsonNull()
    {
        var invocation = JmapMethodInvocation.Create<JmapCore>(
            JmapMethod.Echo,
            JsonDocument.Parse("""{"arg1":null}""").RootElement,
            "c0");

        var r = JsonSerializer.Serialize(invocation);

        r.Should().Be("""["Core/echo",{"arg1":null},"c0"]""");
    }

    [TestMethod]
    public void Create_MethodNotSupportedByObject_ThrowsJmapUnsupportedMethodException()
    {
        var act = () => JmapMethodInvocation.Create<JmapCore>(JmapMethod.Get, JsonDocument.Parse("{}").RootElement, "c0");

        act.Should().Throw<JmapUnsupportedMethodException>();
    }

    [TestMethod]
    public void Deserialize_ThrowsNotSupportedException()
    {
        var act = () => JsonSerializer.Deserialize<JmapMethodInvocation>("""["Core/echo",{},"c0"]""");

        act.Should().Throw<NotSupportedException>();
    }
}
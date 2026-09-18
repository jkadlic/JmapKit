using System.Text.Json;
using FluentAssertions;

namespace JmapKit.Tests;

[TestClass]
public class TestJmapMethodInvocationConverter
{
    [TestMethod]
    public void Serialize_ValidInvocation_ReturnsInvocationString()
    {
        var invocation = new JmapMethodInvocation
        {
            Name = "hello",
            Arguments = new Dictionary<string, object?>
            {
                { "arg1", 3 },
                { "arg2", "foo" },
            },
            CallId = "c0"
        };

        var r = JsonSerializer.Serialize(invocation);

        r.Should().Be("""["hello",{"arg1":3,"arg2":"foo"},"c0"]""");
    }

    [TestMethod]
    public void Serialize_EmptyArguments_WritesEmptyObject()
    {
        var invocation = new JmapMethodInvocation
        {
            Name = "hello",
            Arguments = new Dictionary<string, object?>(),
            CallId = "c0"
        };

        var r = JsonSerializer.Serialize(invocation);

        r.Should().Be("""["hello",{},"c0"]""");
    }

    [TestMethod]
    public void Serialize_NullArgumentValue_WritesJsonNull()
    {
        var invocation = new JmapMethodInvocation
        {
            Name = "hello",
            Arguments = new Dictionary<string, object?> { { "arg1", null } },
            CallId = "c0"
        };

        var r = JsonSerializer.Serialize(invocation);

        r.Should().Be("""["hello",{"arg1":null},"c0"]""");
    }

    [TestMethod]
    public void Deserialize_ThrowsNotSupportedException()
    {
        var act = () => JsonSerializer.Deserialize<JmapMethodInvocation>("""["hello",{},"c0"]""");

        act.Should().Throw<NotSupportedException>();
    }
}
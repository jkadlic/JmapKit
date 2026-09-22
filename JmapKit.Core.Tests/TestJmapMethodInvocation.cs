using System.Text.Json;
using FluentAssertions;

namespace JmapKit.Tests;

[TestClass]
public class TestJmapMethodInvocation
{
    private sealed record TestObject : IJmapObject
    {
        public static string JmapName => "TestObject";
        public static JmapCapability[] JmapCapabilities => [new("urn:test:capability")];
        public static JmapMethod[] SupportedMethods => [JmapMethod.Get];
    }

    private sealed record UnsupportedObject : IJmapObject
    {
        public static string JmapName => "UnsupportedObject";
        public static JmapCapability[] JmapCapabilities => [new("urn:test:capability")];
        public static JmapMethod[] SupportedMethods => [];
    }

    [TestMethod]
    public void Create_SupportedMethod_BuildsNameFromObjectAndMethod()
    {
        var arguments = JsonDocument.Parse("""{"arg1":1}""").RootElement;

        var invocation = JmapMethodInvocation.Create<TestObject>(JmapMethod.Get, arguments, "c0");

        invocation.Name.Should().Be("TestObject/get");
        invocation.Arguments.Should().BeEquivalentTo(arguments);
        invocation.CallId.Should().Be("c0");
    }

    [TestMethod]
    public void Create_UnsupportedMethod_ThrowsJmapUnsupportedMethodException()
    {
        var act = () => JmapMethodInvocation.Create<TestObject>(JmapMethod.Set, JsonDocument.Parse("{}").RootElement, "c0");

        act.Should().Throw<JmapUnsupportedMethodException>();
    }

    [TestMethod]
    public void Create_UnsupportedMethod_ExceptionMessageIncludesObjectAndMethodNames()
    {
        var act = () => JmapMethodInvocation.Create<TestObject>(JmapMethod.Set, JsonDocument.Parse("{}").RootElement, "c0");

        act.Should().Throw<JmapUnsupportedMethodException>()
            .Which.Message.Should().ContainAll("TestObject", "set");
    }

    [TestMethod]
    public void Create_ObjectWithNoSupportedMethods_ThrowsForAnyMethod()
    {
        var act = () => JmapMethodInvocation.Create<UnsupportedObject>(JmapMethod.Echo, JsonDocument.Parse("{}").RootElement, "c0");

        act.Should().Throw<JmapUnsupportedMethodException>();
    }
}
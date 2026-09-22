using System.Text.Json;
using FluentAssertions;

namespace JmapKit.Tests;

[TestClass]
public class TestJmapRequestConverter
{
    private sealed record TestObject : IJmapObject
    {
        public static string JmapName => "TestObject";
        public static JmapCapability[] JmapCapabilities => [new("urn:test:capability")];
        public static JmapMethod[] SupportedMethods => [JmapMethod.Get, JmapMethod.Set];
    }

    [TestMethod]
    public void Serialize_ValidRequest_ReturnsRequestString()
    {
        var invocation = JmapMethodInvocation.Create<JmapCore>(
            JmapMethod.Echo,
            JsonSerializer.SerializeToElement(new Dictionary<string, object?>
            {
                { "arg1", 3 },
                { "arg2", "foo" },
            }),
            "c0");

        var request = new JmapRequest
        {
            Using = [JmapCoreCapability.Core],
            MethodCalls = [invocation]
        };

        var r = JsonSerializer.Serialize(request);

        r.Should().Be("""{"using":["urn:ietf:params:jmap:core"],"methodCalls":[["Core/echo",{"arg1":3,"arg2":"foo"},"c0"]]}""");
    }

    [TestMethod]
    public void Serialize_MultipleMethodCallsAndCapabilities_PreservesOrder()
    {
        var request = new JmapRequest
        {
            Using = [JmapCoreCapability.Core, new JmapCapability("urn:ietf:params:jmap:mail")],
            MethodCalls =
            [
                JmapMethodInvocation.Create<TestObject>(JmapMethod.Get, JsonDocument.Parse("{}").RootElement, "c0"),
                JmapMethodInvocation.Create<TestObject>(JmapMethod.Set, JsonDocument.Parse("{}").RootElement, "c1")
            ]
        };

        var r = JsonSerializer.Serialize(request);

        r.Should().Be(
            """{"using":["urn:ietf:params:jmap:core","urn:ietf:params:jmap:mail"],"methodCalls":[["TestObject/get",{},"c0"],["TestObject/set",{},"c1"]]}""");
    }

    [TestMethod]
    public void Serialize_WithCreatedIds_IncludesCreatedIds()
    {
        var request = new JmapRequest
        {
            Using = [JmapCoreCapability.Core],
            MethodCalls = [],
            CreatedIds = new Dictionary<string, JmapId> { { "clientId1", (JmapId)"serverId1" } }
        };

        var r = JsonSerializer.Serialize(request);

        r.Should().Be(
            """{"using":["urn:ietf:params:jmap:core"],"methodCalls":[],"createdIds":{"clientId1":"serverId1"}}""");
    }

    [TestMethod]
    public void Serialize_WithoutCreatedIds_OmitsCreatedIds()
    {
        var request = new JmapRequest
        {
            Using = [JmapCoreCapability.Core],
            MethodCalls = []
        };

        var r = JsonSerializer.Serialize(request);

        r.Should().NotContain("createdIds");
    }
}
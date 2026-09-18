using System.Text.Json;
using FluentAssertions;

namespace JmapKit.Tests;

[TestClass]
public class TestJmapRequestConverter
{
    [TestMethod]
    public void Serialize_ValidRequest_ReturnsRequestString()
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

        var request = new JmapRequest
        {
            Using = [JmapCoreCapability.Core],
            MethodCalls = [invocation]
        };

        var r = JsonSerializer.Serialize(request);

        r.Should().Be("""{"using":["urn:ietf:params:jmap:core"],"methodCalls":[["hello",{"arg1":3,"arg2":"foo"},"c0"]]}""");
    }

    [TestMethod]
    public void Serialize_MultipleMethodCallsAndCapabilities_PreservesOrder()
    {
        var request = new JmapRequest
        {
            Using = [JmapCoreCapability.Core, new JmapCapability("urn:ietf:params:jmap:mail")],
            MethodCalls =
            [
                new JmapMethodInvocation { Name = "a", Arguments = [], CallId = "c0" },
                new JmapMethodInvocation { Name = "b", Arguments = [], CallId = "c1" }
            ]
        };

        var r = JsonSerializer.Serialize(request);

        r.Should().Be(
            """{"using":["urn:ietf:params:jmap:core","urn:ietf:params:jmap:mail"],"methodCalls":[["a",{},"c0"],["b",{},"c1"]]}""");
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
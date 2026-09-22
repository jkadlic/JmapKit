using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;

namespace JmapKit.Tests;

/// <summary>
/// Tests for <see cref="JmapPartial{T}.MergeOnto"/>.
/// </summary>
[TestClass]
public sealed class TestJmapPartial
{
    /// <summary>
    /// A minimal <see cref="IJmapObject"/> used only to exercise <see cref="JmapPartial{T}"/>.
    /// </summary>
    private sealed record TestObject : IJmapObject
    {
        public static string JmapName => "TestObject";
        public static JmapCapability[] JmapCapabilities => [new("urn:test:capability")];
        public static JmapMethod[] SupportedMethods => [];

        public string? Name { get; init; }
        public long? Count { get; init; }
        public bool? Flag { get; init; }
    }

    private static JsonSerializerOptions Options { get; } = new();

    private static JmapPartial<TestObject> Parse(string json) =>
        JsonSerializer.Deserialize<JmapPartial<TestObject>>(json, Options)!;

    [TestMethod]
    public void MergeOnto_DisjointProperties_MergesAdditively()
    {
        var original = new TestObject { Name = "original" };
        var partial = Parse("""{"Count":3}""");

        var merged = partial.MergeOnto(original, Options);

        merged.Name.Should().Be("original");
        merged.Count.Should().Be(3);
    }

    [TestMethod]
    public void MergeOnto_OverlappingProperty_ServerValueWins()
    {
        var original = new TestObject { Name = "original" };
        var partial = Parse("""{"Name":"server"}""");

        var merged = partial.MergeOnto(original, Options);

        merged.Name.Should().Be("server");
    }

    [TestMethod]
    public void MergeOnto_EmptyPartial_ReturnsEquivalentToOriginal()
    {
        var original = new TestObject { Name = "original", Count = 1, Flag = true };
        var partial = Parse("{}");

        var merged = partial.MergeOnto(original, Options);

        merged.Should().Be(original);
    }

    [TestMethod]
    public void MergeOnto_ExplicitNullProperty_OverwritesWithNull()
    {
        var original = new TestObject { Name = "original" };
        var partial = Parse("""{"Name":null}""");

        var merged = partial.MergeOnto(original, Options);

        merged.Name.Should().BeNull();
    }

    [TestMethod]
    public void MergeOnto_SameInstanceOntoTwoOriginals_DoesNotThrowAndIsIndependent()
    {
        var partial = Parse("""{"Name":"server"}""");
        var first = new TestObject { Count = 1 };
        var second = new TestObject { Count = 2 };

        var mergedFirst = partial.MergeOnto(first, Options);
        var mergedSecond = partial.MergeOnto(second, Options);

        mergedFirst.Count.Should().Be(1);
        mergedSecond.Count.Should().Be(2);
        mergedFirst.Name.Should().Be("server");
        mergedSecond.Name.Should().Be("server");
    }

    [TestMethod]
    public void MergeOnto_UsesProvidedSerializerOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());
        var partial = JsonSerializer.Deserialize<JmapPartial<TestObject>>("""{"Count":5}""", options)!;
        var original = new TestObject();

        var act = () => partial.MergeOnto(original, options);

        act.Should().NotThrow();
    }
}

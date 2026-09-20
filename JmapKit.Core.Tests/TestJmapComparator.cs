using System.Text.Json;
using FluentAssertions;

namespace JmapKit.Tests;

[TestClass]
public sealed class TestJmapComparator
{
    private static JsonSerializerOptions Options { get; } = new();

    [TestMethod]
    public void IsAscending_NotSetInInitializer_DefaultsToTrue()
    {
        var comparator = new JmapComparator { Property = "name" };

        comparator.IsAscending.Should().BeTrue();
    }

    [TestMethod]
    public void Deserialize_MissingIsAscending_DefaultsToTrue()
    {
        var json = """{"Property":"name"}""";

        var comparator = JsonSerializer.Deserialize<JmapComparator>(json, Options);

        comparator.Should().NotBeNull();
        comparator!.IsAscending.Should().BeTrue();
    }

    [TestMethod]
    public void RoundTrip_SerializeThenDeserialize_PreservesValues()
    {
        var original = new JmapComparator { Property = "name", IsAscending = false, Collation = "i;ascii-casemap" };

        var json = JsonSerializer.Serialize(original, Options);
        var back = JsonSerializer.Deserialize<JmapComparator>(json, Options);

        back.Should().Be(original);
    }

    [TestMethod]
    public void Equals_SamePropertyValues_ReturnsTrue()
    {
        var a = new JmapComparator { Property = "name", IsAscending = false, Collation = "x" };
        var b = new JmapComparator { Property = "name", IsAscending = false, Collation = "x" };

        a.Should().Be(b);
    }
}

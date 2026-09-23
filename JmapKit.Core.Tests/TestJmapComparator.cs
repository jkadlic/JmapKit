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
        var json = """{"property":"name"}""";

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
    public void Serialize_CollationNotSet_OmitsCollationRatherThanWritingNull()
    {
        // RFC 8620 §5.5 types collation as "String" with a server-dependent default, not "String|null", so
        // null is not a legal value for it; §3.5 allows omitting it because that default is defined.
        var comparator = new JmapComparator { Property = "name" };

        var json = JsonSerializer.Serialize(comparator, Options);

        json.Should().Be("""{"property":"name","isAscending":true}""");
    }

    [TestMethod]
    public void Serialize_CollationSet_WritesCollation()
    {
        var comparator = new JmapComparator { Property = "name", Collation = "i;ascii-casemap" };

        var json = JsonSerializer.Serialize(comparator, Options);

        json.Should().Be("""{"property":"name","isAscending":true,"collation":"i;ascii-casemap"}""");
    }

    [TestMethod]
    public void Deserialize_ExplicitNullCollation_StillReadsAsNull()
    {
        // WhenWritingNull only suppresses the property on the way out; a server that sends one is still read.
        var json = """{"property":"name","collation":null}""";

        var comparator = JsonSerializer.Deserialize<JmapComparator>(json, Options);

        comparator.Should().NotBeNull();
        comparator!.Collation.Should().BeNull();
    }

    [TestMethod]
    public void Equals_SamePropertyValues_ReturnsTrue()
    {
        var a = new JmapComparator { Property = "name", IsAscending = false, Collation = "x" };
        var b = new JmapComparator { Property = "name", IsAscending = false, Collation = "x" };

        a.Should().Be(b);
    }
}

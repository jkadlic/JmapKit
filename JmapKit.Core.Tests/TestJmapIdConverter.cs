using System.Text.Json;
using FluentAssertions;

namespace JmapKit.Tests;

[TestClass]
public sealed class TestJmapIdConverter
{
    private static JsonSerializerOptions Options { get; } = new();

    // ----- Value position -----

    [TestMethod]
    public void Serialize_AsValue_WritesBareString()
    {
        var id = JmapId.Parse("abc123");

        var json = JsonSerializer.Serialize(id, Options);

        json.Should().Be("\"abc123\"");
    }

    [TestMethod]
    public void Deserialize_AsValue_ValidId_ReturnsId()
    {
        var id = JsonSerializer.Deserialize<JmapId>("\"abc123\"", Options);

        id.ToString().Should().Be("abc123");
    }

    [TestMethod]
    public void Deserialize_AsValue_InvalidId_ThrowsJsonException()
    {
        var act = () => JsonSerializer.Deserialize<JmapId>("\"has space\"", Options);

        act.Should().Throw<JsonException>();
    }

    // ----- Dictionary key position -----

    [TestMethod]
    public void Serialize_DictionaryWithJmapIdKey_WritesBareIdAsPropertyName()
    {
        var dict = new Dictionary<JmapId, string> { [JmapId.Parse("abc123")] = "hello" };

        var json = JsonSerializer.Serialize(dict, Options);

        json.Should().Be("""{"abc123":"hello"}""");
    }

    [TestMethod]
    public void Deserialize_DictionaryWithJmapIdKey_RoundTrips()
    {
        var json = """{"abc123":"hello"}""";

        var dict = JsonSerializer.Deserialize<Dictionary<JmapId, string>>(json, Options);

        dict.Should().NotBeNull();
        dict!.Should().ContainKey(JmapId.Parse("abc123"));
        dict[JmapId.Parse("abc123")].Should().Be("hello");
    }

    [TestMethod]
    public void Deserialize_DictionaryWithInvalidKey_ThrowsJsonException()
    {
        var json = """{"has space":"hello"}""";

        var act = () => JsonSerializer.Deserialize<Dictionary<JmapId, string>>(json, Options);

        act.Should().Throw<JsonException>();
    }

    [TestMethod]
    public void RoundTrip_DictionaryWithMultipleKeys_PreservesAllEntries()
    {
        var dict = new Dictionary<JmapId, int>
        {
            [JmapId.Parse("a")] = 1,
            [JmapId.Parse("b")] = 2
        };

        var json = JsonSerializer.Serialize(dict, Options);
        var back = JsonSerializer.Deserialize<Dictionary<JmapId, int>>(json, Options);

        back.Should().BeEquivalentTo(dict);
    }
}

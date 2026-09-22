using System.Text.Json;
using FluentAssertions;

namespace JmapKit.Tests;

/// <summary>
/// A minimal <see cref="IJmapObject"/> used only to exercise <see cref="JmapPartialConverterFactory"/> and
/// <see cref="JmapPartialConverter{T}"/>.
/// </summary>
public sealed record TestObject : IJmapObject
{
    public static string JmapName => "TestObject";
    public static JmapCapability[] JmapCapabilities => [new("urn:test:capability")];
    public static JmapMethod[] SupportedMethods => [];

    public string? Name { get; init; }
}

[TestClass]
public sealed class TestJmapPartialConverterFactory
{
    private static readonly JmapPartialConverterFactory Factory = new();

    [TestMethod]
    public void CanConvert_ClosedJmapPartial_ReturnsTrue()
    {
        Factory.CanConvert(typeof(JmapPartial<TestObject>)).Should().BeTrue();
    }

    [TestMethod]
    public void CanConvert_UnrelatedType_ReturnsFalse()
    {
        Factory.CanConvert(typeof(TestObject)).Should().BeFalse();
    }

    [TestMethod]
    public void CanConvert_DifferentOpenGenericType_ReturnsFalse()
    {
        Factory.CanConvert(typeof(List<>)).Should().BeFalse();
    }

    [TestMethod]
    public void CanConvert_CollectionOfJmapPartial_ReturnsFalse()
    {
        Factory.CanConvert(typeof(List<JmapPartial<TestObject>>)).Should().BeFalse();
    }

    [TestMethod]
    public void CreateConverter_ReturnsConverterClosedOverSameType()
    {
        var converter = Factory.CreateConverter(typeof(JmapPartial<TestObject>), new JsonSerializerOptions());

        converter.Should().BeOfType<JmapPartialConverter<TestObject>>();
    }
}

[TestClass]
public sealed class TestJmapPartialConverter
{
    private static JsonSerializerOptions Options { get; } = new();

    [TestMethod]
    public void Read_ValidObject_ProducesUsablePartial()
    {
        var partial = JsonSerializer.Deserialize<JmapPartial<TestObject>>("""{"Name":"server"}""", Options);

        partial.Should().NotBeNull();
        partial!.MergeOnto(new TestObject(), Options).Name.Should().Be("server");
    }

    [TestMethod]
    public void Read_EmptyObject_ProducesNoOpPartial()
    {
        var partial = JsonSerializer.Deserialize<JmapPartial<TestObject>>("{}", Options);
        var original = new TestObject { Name = "unchanged" };

        partial!.MergeOnto(original, Options).Should().Be(original);
    }

    [TestMethod]
    [DataRow("[]")]
    [DataRow("\"a string\"")]
    [DataRow("3")]
    public void Read_NonObjectToken_ThrowsJsonException(string json)
    {
        var act = () => JsonSerializer.Deserialize<JmapPartial<TestObject>>(json, Options);

        act.Should().Throw<JsonException>();
    }

    [TestMethod]
    public void Deserialize_TopLevelNull_ReturnsNullWithoutInvokingConverter()
    {
        var partial = JsonSerializer.Deserialize<JmapPartial<TestObject>>("null", Options);

        partial.Should().BeNull();
    }

    [TestMethod]
    public void Write_ThrowsNotSupportedException()
    {
        var partial = JsonSerializer.Deserialize<JmapPartial<TestObject>>("{}", Options)!;

        var act = () => JsonSerializer.Serialize(partial, Options);

        act.Should().Throw<NotSupportedException>();
    }

    // ----- Integration -----

    [TestMethod]
    public void Deserialize_DictionaryOfJmapIdToJmapPartial_Succeeds()
    {
        var json = """{"cid1":{"Name":"server-assigned"}}""";

        var dict = JsonSerializer.Deserialize<Dictionary<JmapId, JmapPartial<TestObject>>>(json, Options);

        dict.Should().NotBeNull();
        dict!.Should().ContainKey(JmapId.Parse("cid1"));
        dict[JmapId.Parse("cid1")].MergeOnto(new TestObject(), Options).Name.Should().Be("server-assigned");
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;

namespace JmapKit.Tests;

[TestClass]
public sealed class TestJmapSerializerOptions
{
    private sealed class InvocationConverter : JsonConverter<JmapMethodInvocation>
    {
        public override JmapMethodInvocation Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o) =>
            throw new NotSupportedException();

        public override void Write(Utf8JsonWriter w, JmapMethodInvocation v, JsonSerializerOptions o) =>
            throw new NotSupportedException();
    }

    private sealed class ResponseConverter : JsonConverter<JmapMethodResponse>
    {
        public override JmapMethodResponse Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o) =>
            throw new NotSupportedException();

        public override void Write(Utf8JsonWriter w, JmapMethodResponse v, JsonSerializerOptions o) =>
            throw new NotSupportedException();
    }

    private sealed class HarmlessConverter : JsonConverter<Guid>
    {
        public override Guid Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o) => Guid.Empty;

        public override void Write(Utf8JsonWriter w, Guid v, JsonSerializerOptions o) => w.WriteStringValue("x");
    }

    [TestMethod]
    public void Ctor_NoConfiguration_AppliesLibraryDefaults()
    {
        var options = new JmapSerializerOptions();

        options.Options.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
    }

    [TestMethod]
    public void Ctor_Configuration_AdjustsDefaultsRatherThanReplacingThem()
    {
        var options = new JmapSerializerOptions(json => json.WriteIndented = true);

        options.Options.WriteIndented.Should().BeTrue();
        options.Options.PropertyNamingPolicy.Should().Be(
            JsonNamingPolicy.CamelCase, "configuring should not discard the library's own defaults");
    }

    [TestMethod]
    public void Ctor_ConverterForOwnType_IsAllowed()
    {
        var act = () => new JmapSerializerOptions(json => json.Converters.Add(new HarmlessConverter()));

        act.Should().NotThrow();
    }

    [TestMethod]
    [DynamicData(nameof(ProtocolConverters))]
    public void Ctor_ConverterClaimingAProtocolType_Throws(JsonConverter converter, string typeName)
    {
        // options.Converters takes precedence over a type's own [JsonConverter], so this would silently
        // replace the positional [name, arguments, callId] encoding rather than customise anything.
        var act = () => new JmapSerializerOptions(json => json.Converters.Add(converter));

        act.Should().Throw<JmapConfigurationException>().WithMessage($"*{typeName}*");
    }

    public static IEnumerable<object[]> ProtocolConverters =>
    [
        [new InvocationConverter(), nameof(JmapMethodInvocation)],
        [new ResponseConverter(), nameof(JmapMethodResponse)],
    ];
}
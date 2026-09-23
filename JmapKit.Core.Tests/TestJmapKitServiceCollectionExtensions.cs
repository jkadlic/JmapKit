using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace JmapKit.Tests;

[TestClass]
public sealed class TestJmapKitServiceCollectionExtensions
{
    private sealed class TestConverter : JsonConverter<Guid>
    {
        public override Guid Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o) => Guid.Empty;

        public override void Write(Utf8JsonWriter w, Guid v, JsonSerializerOptions o) => w.WriteStringValue("x");
    }

    /// <summary>
    /// Builds a provider with the client registered.
    /// </summary>
    /// <remarks>
    /// The credential and options are registered *after* <c>AddJmapClient</c> on purpose. Its existing
    /// guards are no-ops (issue #9), so registering first would be shadowed by its own
    /// <c>AddSingleton&lt;JmapTokenCredential&gt;()</c>, whose parameterless constructor throws without
    /// <c>JMAP_TOKEN</c> set. Registering last wins either way, so this keeps the test independent of that
    /// bug and of ambient environment variables.
    /// </remarks>
    private static ServiceProvider BuildProvider(Action<JsonSerializerOptions>? configureJson = null)
    {
        var services = new ServiceCollection();
        services.AddJmapClient(configureJson);
        services.AddSingleton(new JmapClientOptions("jmap.example.com"));
        services.AddSingleton(new JmapTokenCredential("test-token"));
        return services.BuildServiceProvider(validateScopes: true);
    }

    [TestMethod]
    public void AddJmapClient_ResolvesTheClient()
    {
        using var provider = BuildProvider();

        var client = provider.GetRequiredService<IJmapClient>();

        client.Should().BeOfType<JmapClient>();
    }

    [TestMethod]
    public void AddJmapClient_RegistersSerializerOptionsWithTheLibraryDefaults()
    {
        using var provider = BuildProvider();

        var options = provider.GetRequiredService<JmapSerializerOptions>();

        options.Options.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
    }

    [TestMethod]
    public void AddJmapClient_ConfigureJson_IsAppliedToTheRegisteredOptions()
    {
        using var provider = BuildProvider(json => json.Converters.Add(new TestConverter()));

        var options = provider.GetRequiredService<JmapSerializerOptions>();

        options.Options.Converters.Should().ContainSingle().Which.Should().BeOfType<TestConverter>();
        options.Options.PropertyNamingPolicy.Should().Be(
            JsonNamingPolicy.CamelCase, "configuring should adjust the defaults rather than replace them");
    }

    /// <summary>
    /// <see cref="JsonSerializerOptions"/> caches type metadata per instance, so resolving a new one per
    /// client would discard that cache on every resolve.
    /// </summary>
    [TestMethod]
    public void AddJmapClient_SerializerOptions_AreASingleSharedInstance()
    {
        using var provider = BuildProvider();

        var first = provider.GetRequiredService<JmapSerializerOptions>();
        var second = provider.GetRequiredService<JmapSerializerOptions>();

        second.Should().BeSameAs(first);
        second.Options.Should().BeSameAs(first.Options);
    }
}

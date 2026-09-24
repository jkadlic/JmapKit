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
    /// The credential and options are registered explicitly so the tests never depend on ambient
    /// <c>JMAP_TOKEN</c>/<c>JMAP_HOST</c> environment variables, whose absence would make the defaults'
    /// parameterless constructors throw.
    /// </remarks>
    private static ServiceProvider BuildProvider(Action<JsonSerializerOptions>? configureJson = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new JmapClientOptions("jmap.example.com"));
        services.AddSingleton(new JmapTokenCredential("test-token"));
        services.AddJmapClient(configureJson);
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

    /// <summary>
    /// Regression test for issue #9: the defaults must not shadow what the caller already registered.
    /// </summary>
    [TestMethod]
    public void AddJmapClient_DoesNotShadowExplicitlyRegisteredOptionsAndCredential()
    {
        var options = new JmapClientOptions("jmap.example.com");
        var credential = new JmapTokenCredential("test-token");
        var serializerOptions = new JmapSerializerOptions();

        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddSingleton(credential);
        services.AddSingleton(serializerOptions);
        services.AddJmapClient();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        provider.GetRequiredService<JmapClientOptions>().Should().BeSameAs(options);
        provider.GetRequiredService<JmapTokenCredential>().Should().BeSameAs(credential);
        provider.GetRequiredService<JmapSerializerOptions>().Should().BeSameAs(serializerOptions);
    }

    /// <summary>
    /// Guarding the registrations must not stop them happening: with nothing registered by the caller, the
    /// environment-variable defaults are still the ones wired up.
    /// </summary>
    [TestMethod]
    public void AddJmapClient_WithoutExplicitRegistrations_RegistersTheEnvironmentDefaults()
    {
        var services = new ServiceCollection();
        services.AddJmapClient();

        services.Should().ContainSingle(d =>
            d.ServiceType == typeof(JmapClientOptions) && d.ImplementationType == typeof(JmapClientOptions));
        services.Should().ContainSingle(d =>
            d.ServiceType == typeof(JmapTokenCredential) && d.ImplementationType == typeof(JmapTokenCredential));
    }

    /// <summary>
    /// The registrations are guarded rather than appended, so a second call cannot leave a default
    /// descriptor sitting after the caller's own.
    /// </summary>
    [TestMethod]
    public void AddJmapClient_CalledTwice_DoesNotDuplicateTheDefaults()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new JmapClientOptions("jmap.example.com"));
        services.AddSingleton(new JmapTokenCredential("test-token"));
        services.AddJmapClient();
        services.AddJmapClient();

        services.Should().ContainSingle(d => d.ServiceType == typeof(JmapClientOptions));
        services.Should().ContainSingle(d => d.ServiceType == typeof(JmapTokenCredential));
        services.Should().ContainSingle(d => d.ServiceType == typeof(JmapSerializerOptions));
    }
}

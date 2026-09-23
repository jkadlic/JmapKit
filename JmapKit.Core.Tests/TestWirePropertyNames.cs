using System.Reflection;
using System.Text.Json.Serialization;
using FluentAssertions;

namespace JmapKit.Tests;

/// <summary>
/// Guards the contract documented on <see cref="IJmapObject"/>: every property this library puts on the wire
/// names itself explicitly, rather than relying on the camelCase fallback policy.
/// </summary>
[TestClass]
public sealed class TestWirePropertyNames
{
    /// <summary>
    /// Types in the JmapKit namespace that carry properties but never reach the wire. Anything added here
    /// needs a reason; everything else is expected to be annotated.
    /// </summary>
    private static readonly Dictionary<Type, string> NotWireTypes = new()
    {
        [typeof(JmapClientOptions)] = "client configuration, never serialized",
        [typeof(JmapTokenCredential)] = "credential holder, never serialized",
        [typeof(JmapSerializerOptions)] = "serializer configuration, never serialized",
    };

    private static IEnumerable<Type> WireTypes() =>
        typeof(IJmapObject).Assembly
            .GetExportedTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } || t.IsValueType)
            .Where(t => !t.IsEnum)
            .Where(t => !typeof(Exception).IsAssignableFrom(t))
            // Converters are machinery, not payloads; their inherited Type/HandleNull are not wire properties.
            .Where(t => !typeof(JsonConverter).IsAssignableFrom(t))
            // Types with their own converter control their JSON shape entirely, so property names are
            // irrelevant to them (JmapId, JmapCapability, the positional tuple converters, JmapPartial).
            .Where(t => t.GetCustomAttribute<JsonConverterAttribute>() is null)
            .Where(t => !NotWireTypes.ContainsKey(t))
            .Where(t => SerializedProperties(t).Any());

    private static IEnumerable<PropertyInfo> SerializedProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetMethod is not null)
            .Where(p => p.GetIndexParameters().Length == 0)
            .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() is null);

    [TestMethod]
    public void EveryWireProperty_DeclaresAnExplicitJsonPropertyName()
    {
        var missing = WireTypes()
            .SelectMany(t => SerializedProperties(t).Select(p => (Type: t, Property: p)))
            .Where(x => x.Property.GetCustomAttribute<JsonPropertyNameAttribute>() is null)
            .Select(x => $"{x.Type.Name}.{x.Property.Name}")
            .OrderBy(n => n)
            .ToList();

        missing.Should().BeEmpty(
            "every serialized property must name itself with [JsonPropertyName]; JMAP property names are " +
            "fixed literals from the spec, not a transform of the C# identifier. Add the attribute, or add " +
            "the type to NotWireTypes with a reason if it never reaches the wire");
    }

    [TestMethod]
    public void EveryWirePropertyName_IsLowerCamelCase()
    {
        var offenders = WireTypes()
            .SelectMany(t => SerializedProperties(t).Select(p => (Type: t, Property: p)))
            .Select(x => (x.Type, x.Property, Name: x.Property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name))
            .Where(x => x.Name is not null && !char.IsLower(x.Name[0]))
            .Select(x => $"{x.Type.Name}.{x.Property.Name} -> \"{x.Name}\"")
            .OrderBy(n => n)
            .ToList();

        offenders.Should().BeEmpty(
            "every JMAP property name defined in RFC 8620 begins with a lowercase letter; a capitalised one " +
            "is more likely a transcription slip than a real spec name");
    }

    [TestMethod]
    public void WireTypes_AreDiscovered()
    {
        // Guards the discovery query itself: if a filter above silently matches nothing, both tests above
        // would pass vacuously.
        WireTypes().Should().HaveCountGreaterThan(10);
    }
}

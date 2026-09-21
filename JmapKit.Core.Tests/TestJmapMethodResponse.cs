using System.Text.Json;
using FluentAssertions;

namespace JmapKit.Tests;

[TestClass]
public sealed class TestJmapMethodResponse
{
    private static JsonSerializerOptions Options { get; } = new();

    private sealed record TestResult
    {
        public required string Value { get; init; }
    }

    private static JmapMethodResponse Response(string name, string argumentsJson) => new()
    {
        Name = name,
        Arguments = JsonDocument.Parse(argumentsJson).RootElement,
        CallId = "c0"
    };

    [TestMethod]
    public void IsError_NameIsError_ReturnsTrue()
    {
        var response = Response("error", """{"Type":"unknownMethod"}""");

        response.IsError.Should().BeTrue();
    }

    [TestMethod]
    public void IsError_NameIsMethodName_ReturnsFalse()
    {
        var response = Response("Foo/get", "{}");

        response.IsError.Should().BeFalse();
    }

    [TestMethod]
    public void TryDeserialize_SuccessResponse_ReturnsTrueAndPopulatesValue()
    {
        var response = Response("Foo/get", """{"Value":"hello"}""");

        var success = response.TryDeserialize<TestResult>(Options, out var value, out var error);

        success.Should().BeTrue();
        value.Should().NotBeNull();
        value!.Value.Should().Be("hello");
        error.Should().BeNull();
    }

    [TestMethod]
    public void TryDeserialize_ErrorResponse_ReturnsFalseAndPopulatesError()
    {
        var response = Response("error", """{"Type":"invalidArguments","Description":"bad stuff"}""");

        var success = response.TryDeserialize<TestResult>(Options, out var value, out var error);

        success.Should().BeFalse();
        value.Should().BeNull();
        error.Should().NotBeNull();
        error!.Type.Should().Be("invalidArguments");
        error.Description.Should().Be("bad stuff");
    }

    [TestMethod]
    public void TryDeserialize_ErrorResponseWithoutDescription_DescriptionIsNull()
    {
        var response = Response("error", """{"Type":"unknownMethod"}""");

        response.TryDeserialize<TestResult>(Options, out _, out var error);

        error!.Description.Should().BeNull();
    }
}
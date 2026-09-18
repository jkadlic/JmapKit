using System.Text.Json;
using FluentAssertions;

namespace JmapKit.Tests;

[TestClass]
public class TestJmapSession
{
    private static JsonSerializerOptions Options { get; } = new();

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        Options.Converters.Add(new JmapObjectConverter());
    }

    private static readonly Dictionary<string, object?> ValidSessionFields = new()
    {
        ["username"] = "user@example.com",
        ["apiUrl"] = "https://api.example.com/jmap/api/",
        ["uploadUrl"] = "https://api.example.com/jmap/upload/{accountId}/",
        ["downloadUrl"] = "https://api.example.com/jmap/download/{accountId}/{blobId}/{name}?type={type}",
        ["eventSourceUrl"] = "https://api.example.com/jmap/event/",
        ["state"] = "state-0",
        ["accounts"] = new Dictionary<string, object?>
        {
            ["acc1"] = new Dictionary<string, object?>
            {
                ["name"] = "user@example.com",
                ["isPersonal"] = true,
                ["isReadOnly"] = false,
                ["accountCapabilities"] = new Dictionary<string, object?>
                {
                    ["urn:ietf:params:jmap:mail"] = new Dictionary<string, object?>
                    {
                        ["maxMailboxesPerEmail"] = 1000
                    }
                }
            }
        },
        ["primaryAccounts"] = new Dictionary<string, object?>
        {
            ["urn:ietf:params:jmap:mail"] = "acc1"
        },
        ["capabilities"] = new Dictionary<string, object?>
        {
            ["urn:ietf:params:jmap:core"] = new Dictionary<string, object?>
            {
                ["maxSizeUpload"] = 250000000,
                ["collationAlgorithms"] = new List<object?> { "i;ascii-numeric", "i;ascii-casemap" }
            }
        }
    };

    private static string BuildSessionJson(params string[] fieldsToOmit)
    {
        var fields = new Dictionary<string, object?>(ValidSessionFields);
        foreach (var field in fieldsToOmit)
            fields.Remove(field);

        return JsonSerializer.Serialize(fields);
    }

    [TestMethod]
    public void Deserialize_ValidSession_PopulatesAllFields()
    {
        var session = JsonSerializer.Deserialize<JmapSession>(BuildSessionJson(), Options);

        session.Should().NotBeNull();
        session.Username.Should().Be("user@example.com");
        session.ApiUrl.Should().Be("https://api.example.com/jmap/api/");
        session.UploadUrl.Should().Be("https://api.example.com/jmap/upload/{accountId}/");
        session.DownloadUrl.Should().Be("https://api.example.com/jmap/download/{accountId}/{blobId}/{name}?type={type}");
        session.EventSourceUrl.Should().Be("https://api.example.com/jmap/event/");
        session.State.Should().Be("state-0");
    }

    [TestMethod]
    public void Deserialize_Accounts_KeyedByAccountId()
    {
        var session = JsonSerializer.Deserialize<JmapSession>(BuildSessionJson(), Options);

        session.Should().NotBeNull();
        session.Accounts.Should().ContainKey("acc1");
        var account = session.Accounts["acc1"];
        account.Name.Should().Be("user@example.com");
        account.IsPersonal.Should().BeTrue();
        account.IsReadOnly.Should().BeFalse();
    }

    [TestMethod]
    public void Deserialize_AccountCapabilities_KeyedByCapabilityUri()
    {
        var session = JsonSerializer.Deserialize<JmapSession>(BuildSessionJson(), Options);

        session.Should().NotBeNull();
        var mailCapability = session.Accounts["acc1"].AccountCapabilities["urn:ietf:params:jmap:mail"];
        mailCapability["maxMailboxesPerEmail"].Should().Be(1000);
    }

    [TestMethod]
    public void Deserialize_AccountWithoutAccountCapabilities_ReturnsEmptyDictionary()
    {
        var fields = new Dictionary<string, object?>(ValidSessionFields);
        var accounts = new Dictionary<string, object?>
        {
            ["acc1"] = new Dictionary<string, object?>
            {
                ["name"] = "user@example.com",
                ["isPersonal"] = true,
                ["isReadOnly"] = false
            }
        };
        fields["accounts"] = accounts;

        var session = JsonSerializer.Deserialize<JmapSession>(JsonSerializer.Serialize(fields), Options);

        session.Should().NotBeNull();
        session.Accounts["acc1"].AccountCapabilities.Should().BeEmpty();
    }

    [TestMethod]
    public void Deserialize_PrimaryAccounts_KeyedByCapabilityUri()
    {
        var session = JsonSerializer.Deserialize<JmapSession>(BuildSessionJson(), Options);

        session.Should().NotBeNull();
        session.PrimaryAccounts["urn:ietf:params:jmap:mail"].Should().Be("acc1");
    }

    [TestMethod]
    public void Deserialize_Capabilities_ParsesNestedArguments()
    {
        var session = JsonSerializer.Deserialize<JmapSession>(BuildSessionJson(), Options);

        session.Should().NotBeNull();
        var core = session.Capabilities["urn:ietf:params:jmap:core"];
        core["maxSizeUpload"].Should().Be(250000000);
        var algorithms = core["collationAlgorithms"].Should().BeOfType<List<object?>>().Subject;
        algorithms.Should().BeEquivalentTo(new object?[] { "i;ascii-numeric", "i;ascii-casemap" });
    }

    [TestMethod]
    [DataRow("username")]
    [DataRow("apiUrl")]
    [DataRow("uploadUrl")]
    [DataRow("downloadUrl")]
    [DataRow("eventSourceUrl")]
    [DataRow("state")]
    [DataRow("accounts")]
    [DataRow("primaryAccounts")]
    [DataRow("capabilities")]
    public void Deserialize_MissingRequiredProperty_ThrowsJsonException(string fieldToOmit)
    {
        var json = BuildSessionJson(fieldToOmit);

        var act = () => JsonSerializer.Deserialize<JmapSession>(json, Options);

        act.Should().Throw<JsonException>();
    }
}
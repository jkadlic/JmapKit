using FluentAssertions;

namespace JmapKit.Tests;

/// <summary>
/// Tests for <see cref="JmapId"/> against the "id" data type defined in
/// <see href="https://www.rfc-editor.org/rfc/rfc8620.html#section-1.2">RFC 8620 §1.2</see>.
/// </summary>
[TestClass]
public class TestJmapId
{
    // ----- Parsing: positive -----

    [TestMethod]
    [DataRow("a")]
    [DataRow("A")]
    [DataRow("0")]
    [DataRow("-")]
    [DataRow("_")]
    [DataRow("abcXYZ019-_")]
    [DataRow("f42-message-id_2024")]
    public void TryParse_ValidId_ReturnsTrueAndPopulatesResult(string input)
    {
        var success = JmapId.TryParse(input, out var id);

        success.Should().BeTrue();
        id.ToString().Should().Be(input);
    }

    [TestMethod]
    public void TryParse_MinimumLength_ReturnsTrue()
    {
        var success = JmapId.TryParse("x", out var id);

        success.Should().BeTrue();
        id.ToString().Should().Be("x");
    }

    [TestMethod]
    public void TryParse_MaximumLength_ReturnsTrue()
    {
        var input = new string('a', 255);

        var success = JmapId.TryParse(input, out var id);

        success.Should().BeTrue();
        id.ToString().Should().Be(input);
    }

    [TestMethod]
    public void TryParse_FullAllowedAlphabet_ReturnsTrue()
    {
        const string allowed = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

        var success = JmapId.TryParse(allowed, out var id);

        success.Should().BeTrue();
        id.ToString().Should().Be(allowed);
    }

    [TestMethod]
    public void Parse_ValidId_ReturnsId()
    {
        var id = JmapId.Parse("valid-id_1");

        id.ToString().Should().Be("valid-id_1");
    }

    // ----- Parsing: negative -----

    [TestMethod]
    public void TryParse_Null_ReturnsFalse()
    {
        var success = JmapId.TryParse(null, out var id);

        success.Should().BeFalse();
        id.Equals(default(JmapId)).Should().BeTrue();
    }

    [TestMethod]
    public void TryParse_EmptyString_ReturnsFalse()
    {
        var success = JmapId.TryParse("", out _);

        success.Should().BeFalse();
    }

    [TestMethod]
    public void TryParse_TooLong_ReturnsFalse()
    {
        var input = new string('a', 256);

        var success = JmapId.TryParse(input, out _);

        success.Should().BeFalse();
    }

    [TestMethod]
    [DataRow(" ")]
    [DataRow("has space")]
    [DataRow("has.dot")]
    [DataRow("has+plus")]
    [DataRow("has/slash")]
    [DataRow("has=equals")]
    [DataRow("has@at")]
    [DataRow("has:colon")]
    [DataRow("has\nnewline")]
    [DataRow("café")]
    [DataRow("😀")]
    public void TryParse_DisallowedCharacters_ReturnsFalse(string input)
    {
        var success = JmapId.TryParse(input, out _);

        success.Should().BeFalse();
    }

    [TestMethod]
    public void Parse_InvalidId_ThrowsFormatException()
    {
        var act = () => JmapId.Parse("has space");

        act.Should().Throw<FormatException>();
    }

    [TestMethod]
    public void Parse_EmptyString_ThrowsFormatException()
    {
        var act = () => JmapId.Parse("");

        act.Should().Throw<FormatException>();
    }

    // ----- Parsing: edge cases -----

    [TestMethod]
    public void TryParse_AllDigitId_ReturnsTrue()
    {
        var success = JmapId.TryParse("123456", out var id);

        success.Should().BeTrue();
        id.ToString().Should().Be("123456");
    }

    [TestMethod]
    public void TryParse_LeadingDash_ReturnsTrue()
    {
        var success = JmapId.TryParse("-leading-dash", out var id);

        success.Should().BeTrue();
        id.ToString().Should().Be("-leading-dash");
    }

    [TestMethod]
    public void TryParse_ReservedNilSequence_ReturnsTrue()
    {
        var success = JmapId.TryParse("NIL", out var id);

        success.Should().BeTrue();
        id.ToString().Should().Be("NIL");
    }

    // ----- Casting -----

    [TestMethod]
    public void ImplicitCastToString_ReturnsUnderlyingValue()
    {
        var id = JmapId.Parse("abc123");

        string value = id;

        value.Should().Be("abc123");
    }

    [TestMethod]
    public void ImplicitCastToString_DefaultId_ReturnsEmptyString()
    {
        var id = default(JmapId);

        string value = id;

        value.Should().Be(string.Empty);
    }

    [TestMethod]
    public void ExplicitCastFromString_ValidId_ProducesEquivalentId()
    {
        var id = (JmapId)"abc123";

        id.ToString().Should().Be("abc123");
        id.Equals(JmapId.Parse("abc123")).Should().BeTrue();
    }

    [TestMethod]
    public void ExplicitCastFromString_InvalidId_ThrowsFormatException()
    {
        var act = () => (JmapId)"not a valid id!";

        act.Should().Throw<FormatException>();
    }

    [TestMethod]
    public void RoundTrip_CastToStringAndBack_PreservesValue()
    {
        var original = JmapId.Parse("round-trip_42");

        string asString = original;
        var roundTripped = (JmapId)asString;

        roundTripped.Equals(original).Should().BeTrue();
    }

    // ----- Comparing -----

    [TestMethod]
    public void Equals_SameValue_ReturnsTrue()
    {
        var a = JmapId.Parse("same-id");
        var b = JmapId.Parse("same-id");

        a.Equals(b).Should().BeTrue();
    }

    [TestMethod]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        var a = JmapId.Parse("id-a");
        var b = JmapId.Parse("id-b");

        a.Equals(b).Should().BeFalse();
    }

    [TestMethod]
    public void Equals_DiffersOnlyByCase_ReturnsFalse()
    {
        var lower = JmapId.Parse("abc");
        var upper = JmapId.Parse("ABC");

        lower.Equals(upper).Should().BeFalse();
    }

    [TestMethod]
    public void Equals_DefaultIds_ReturnsTrue()
    {
        default(JmapId).Equals(default(JmapId)).Should().BeTrue();
    }

    [TestMethod]
    public void CompareTo_EqualIds_ReturnsZero()
    {
        var a = JmapId.Parse("same");
        var b = JmapId.Parse("same");

        a.CompareTo(b).Should().Be(0);
    }

    [TestMethod]
    public void CompareTo_UsesOrdinalCharacterOrder()
    {
        // ASCII ordering: '-' (0x2D) < digits (0x30-0x39) < 'A'-'Z' (0x41-0x5A)
        //   < '_' (0x5F) < 'a'-'z' (0x61-0x7A)
        var dash = JmapId.Parse("-x");
        var digit = JmapId.Parse("0x");
        var upper = JmapId.Parse("Ax");
        var underscore = JmapId.Parse("_x");
        var lower = JmapId.Parse("ax");

        dash.CompareTo(digit).Should().BeLessThan(0);
        digit.CompareTo(upper).Should().BeLessThan(0);
        upper.CompareTo(underscore).Should().BeLessThan(0);
        underscore.CompareTo(lower).Should().BeLessThan(0);
    }

    [TestMethod]
    public void CompareTo_PrefixOfLongerId_ReturnsNegative()
    {
        var shorter = JmapId.Parse("abc");
        var longer = JmapId.Parse("abcd");

        shorter.CompareTo(longer).Should().BeLessThan(0);
        longer.CompareTo(shorter).Should().BeGreaterThan(0);
    }

    // ----- Object equality overrides -----

    [TestMethod]
    public void ObjectEquals_SameValue_ReturnsTrue()
    {
        var a = JmapId.Parse("same-id");
        object b = JmapId.Parse("same-id");

        a.Equals(b).Should().BeTrue();
    }

    [TestMethod]
    public void ObjectEquals_DifferentValue_ReturnsFalse()
    {
        var a = JmapId.Parse("id-a");
        object b = JmapId.Parse("id-b");

        a.Equals(b).Should().BeFalse();
    }

    [TestMethod]
    public void ObjectEquals_DifferentType_ReturnsFalse()
    {
        var a = JmapId.Parse("abc");

        a.Equals("abc").Should().BeFalse();
    }

    [TestMethod]
    public void ObjectEquals_Null_ReturnsFalse()
    {
        var a = JmapId.Parse("abc");

        a.Equals((object?)null).Should().BeFalse();
    }

    [TestMethod]
    public void GetHashCode_EqualValues_ReturnsSameHashCode()
    {
        var a = JmapId.Parse("same-id");
        var b = JmapId.Parse("same-id");

        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [TestMethod]
    public void GetHashCode_DefaultId_DoesNotThrow()
    {
        var act = () => default(JmapId).GetHashCode();

        act.Should().NotThrow();
    }

    [TestMethod]
    public void Hashtable_BoxedLookup_UsesObjectEqualsAndGetHashCodeOverrides()
    {
        var table = new System.Collections.Hashtable
        {
            [JmapId.Parse("key")] = "value"
        };

        table[JmapId.Parse("key")].Should().Be("value");
    }

    // ----- LINQ usage -----

    [TestMethod]
    public void OrderBy_SortsIdsOrdinally()
    {
        string[] sourceArray = ["banana", "Banana", "-banana", "1banana", "_banana"];
        var ids = sourceArray.Select(JmapId.Parse).ToList();

        var sorted = ids.OrderBy(id => id).Select(id => id.ToString()).ToList();

        sorted.Should().Equal("-banana", "1banana", "Banana", "_banana", "banana");
    }

    [TestMethod]
    public void Distinct_RemovesValueEqualIds()
    {
        string[] sourceArray = ["a", "b", "a", "c", "b"];
        var ids = sourceArray.Select(JmapId.Parse);

        var distinct = ids.Distinct().Select(id => id.ToString()).ToList();

        distinct.Should().BeEquivalentTo(["a", "b", "c"]);
    }

    [TestMethod]
    public void Distinct_TreatsDifferentCaseAsDistinct()
    {
        string[] sourceArray = ["abc", "ABC"];
        var ids = sourceArray.Select(JmapId.Parse);

        var distinct = ids.Distinct();

        distinct.Should().HaveCount(2);
    }

    [TestMethod]
    public void GroupBy_GroupsValueEqualIds()
    {
        var records = new[]
        {
            (Id: JmapId.Parse("m1"), Subject: "first"),
            (Id: JmapId.Parse("m1"), Subject: "duplicate"),
            (Id: JmapId.Parse("m2"), Subject: "second")
        };

        var groups = records.GroupBy(r => r.Id).ToList();

        groups.Should().HaveCount(2);
        groups.Single(g => g.Key.Equals(JmapId.Parse("m1"))).Should().HaveCount(2);
    }

    [TestMethod]
    public void Contains_FindsValueEqualId()
    {
        string[] sourceArray = ["a", "b", "c"];
        var ids = sourceArray.Select(JmapId.Parse).ToList();

        ids.Contains(JmapId.Parse("b")).Should().BeTrue();
        ids.Contains(JmapId.Parse("z")).Should().BeFalse();
    }

    [TestMethod]
    public void Select_CastsIdsToStrings()
    {
        string[] sourceArray = ["a", "b", "c"];
        var ids = sourceArray.Select(JmapId.Parse).ToList();

        var strings = ids.Select(id => (string)id).ToList();

        strings.Should().Equal("a", "b", "c");
    }

    [TestMethod]
    public void Where_FiltersUsingImplicitCast()
    {
        string[] sourceArray = ["a1", "b2", "a3"];
        var ids = sourceArray.Select(JmapId.Parse).ToList();

        var filtered = ids.Where(id => ((string)id).StartsWith('a')).ToList();

        filtered.Should().HaveCount(2);
    }

    [TestMethod]
    public void ToDictionary_LookupByValueEqualId_Succeeds()
    {
        string[] sourceArray = ["a", "b", "c"];
        var ids = sourceArray.Select(JmapId.Parse).ToList();
        var lookup = ids.ToDictionary(id => id, id => id.ToString().ToUpperInvariant());

        lookup[JmapId.Parse("b")].Should().Be("B");
    }

    [TestMethod]
    public void ToHashSet_DeduplicatesValueEqualIds()
    {
        string[] sourceArray = ["a", "b", "a", "c"];
        var ids = sourceArray.Select(JmapId.Parse);

        var set = ids.ToHashSet();

        set.Should().HaveCount(3);
        set.Contains(JmapId.Parse("a")).Should().BeTrue();
    }
}

using UniTracks.Services.Changelog;

namespace UniTracks.Tests.Changelog;

/// <summary>
/// The changelog decides which release notes are newer than the version the user last saw, so the
/// comparison has to survive the version spellings that occur in practice: "0.4", "0.4.0",
/// "0.3.1", "0.10" and suffixes such as "0.4-beta1".
/// </summary>
public sealed class ChangelogVersionTests
{
    [Theory]
    [InlineData("0.4", "0.4", 0)]
    [InlineData("0.4", "0.4.0", 0)]
    [InlineData("0.4.0.0", "0.4", 0)]
    [InlineData("0.4-beta1", "0.4", 0)]
    [InlineData("0.5", "0.4", 1)]
    [InlineData("0.4", "0.5", -1)]
    [InlineData("0.3.1", "0.4", -1)]
    [InlineData("0.10", "0.9", 1)]
    [InlineData("1.0", "0.99.99", 1)]
    public void Compare_OrdersDottedVersionsNumerically(string left, string right, int expected)
    {
        var result = ChangelogVersion.Compare(left, right);

        Assert.Equal(expected, Math.Sign(result));
    }

    [Fact]
    public void Compare_TreatsAMissingVersionAsOldest()
    {
        Assert.True(ChangelogVersion.Compare(null, "0.1") < 0);
        Assert.True(ChangelogVersion.Compare("0.1", null) > 0);
        Assert.Equal(0, ChangelogVersion.Compare(null, null));
        Assert.Equal(0, ChangelogVersion.Compare(string.Empty, "   "));
    }

    [Fact]
    public void IsSame_IgnoresTrailingZeroSegments()
    {
        Assert.True(ChangelogVersion.IsSame("0.5", "0.5.0"));
        Assert.False(ChangelogVersion.IsSame("0.5", "0.4"));
        Assert.False(ChangelogVersion.IsSame(null, "0.5"));
    }

    [Theory]
    [InlineData("0.4", true)]
    [InlineData("0.4.0.0", true)]
    [InlineData("v1", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void IsValid_RequiresAtLeastOneNumericSegment(string? version, bool expected)
    {
        Assert.Equal(expected, ChangelogVersion.IsValid(version));
    }
}

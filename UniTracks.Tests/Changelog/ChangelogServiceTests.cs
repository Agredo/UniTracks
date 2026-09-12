using UniTracks.Services.Changelog;

namespace UniTracks.Tests.Changelog;

/// <summary>
/// Guards the shipped <c>Changelog/changelog.json</c>: it is embedded as a resource, so a wrong
/// resource name or a typo in the file would silently produce an empty "what's new" popup.
/// </summary>
public sealed class ChangelogServiceTests
{
    private static readonly ChangelogService Service = new();

    [Fact]
    public void Releases_AreLoadedFromTheEmbeddedFile()
    {
        Assert.NotEmpty(Service.Releases);
    }

    [Fact]
    public void Releases_AreSortedNewestFirstWithoutDuplicates()
    {
        var versions = Service.Releases.Select(release => release.Version).ToArray();

        Assert.Equal(versions.Distinct(StringComparer.OrdinalIgnoreCase).Count(), versions.Length);

        for (var index = 1; index < versions.Length; index++)
        {
            Assert.True(
                ChangelogVersion.Compare(versions[index - 1], versions[index]) > 0,
                $"'{versions[index - 1]}' muss neuer sein als '{versions[index]}'.");
        }
    }

    [Fact]
    public void Releases_AreAllUsableByThePopup()
    {
        foreach (var release in Service.Releases)
        {
            Assert.True(ChangelogVersion.IsValid(release.Version), $"Ungueltige Version '{release.Version}'.");
            Assert.False(string.IsNullOrWhiteSpace(release.Title), $"'{release.Version}' hat keinen Titel.");
            Assert.NotEmpty(release.Changes);
            Assert.All(release.Changes, change => Assert.False(string.IsNullOrWhiteSpace(change.Text)));

            if (release.Date.Length > 0)
            {
                Assert.True(
                    DateTime.TryParse(release.Date, out _),
                    $"'{release.Version}' hat ein unlesbares Datum '{release.Date}'.");
            }
        }
    }

    [Fact]
    public void Releases_ContainAtLeastOneOfEveryChangeKind()
    {
        var kinds = Service.Releases.SelectMany(release => release.Changes).Select(change => change.Kind).ToArray();

        Assert.Contains(ChangelogChangeKind.Feature, kinds);
        Assert.Contains(ChangelogChangeKind.Improvement, kinds);
        Assert.Contains(ChangelogChangeKind.Fix, kinds);
    }

    [Fact]
    public void Find_ReturnsTheReleaseOfTheCurrentVersion()
    {
        var newest = Service.Releases[0];

        Assert.Same(newest, Service.Find(newest.Version));
        Assert.Same(newest, Service.Find(newest.Version.ToUpperInvariant()));
    }

    [Fact]
    public void Find_MatchesTheSameVersionSpelledDifferently()
    {
        var newest = Service.Releases[0];

        Assert.Same(newest, Service.Find($"{newest.Version}.0"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("99.9")]
    public void Find_ReturnsNothingForAnUnknownVersion(string? version)
    {
        Assert.Null(Service.Find(version));
    }

    [Fact]
    public void GetSince_ReturnsEverythingFromTheGivenVersionUpwards()
    {
        var newest = Service.Releases[0];

        Assert.Same(newest, Assert.Single(Service.GetSince(newest.Version)));

        var everything = Service.GetSince(null);
        Assert.Equal(Service.Releases.Count, everything.Count);

        // The version right below the newest must widen the result by exactly that release.
        if (Service.Releases.Count > 1)
        {
            var second = Service.Releases[1];
            Assert.Equal(2, Service.GetSince(second.Version).Count);
        }
    }

    [Fact]
    public void GetSince_IgnoresAVersionItCannotRead()
    {
        Assert.Equal(Service.Releases.Count, Service.GetSince("unbekannt").Count);
    }
}

using UniTracks.Services.Changelog;

namespace UniTracks.Tests.Changelog;

/// <summary>
/// The changelog file is maintained by hand, so parsing has to be forgiving: a typo must never take
/// the app down, and the older spellings of the file have to keep working.
/// </summary>
public sealed class ChangelogCatalogTests
{
    [Fact]
    public void Parse_ReadsTheNestedReleasesShape()
    {
        var releases = ChangelogCatalog.Parse(
            """
            {
              "releases": [
                {
                  "version": "1.2",
                  "date": "2026-01-02",
                  "title": "Titel",
                  "changes": [
                    { "kind": "feature", "text": "Neu" },
                    { "kind": "fix", "text": "Behoben" },
                    { "kind": "improvement", "text": "Geändert" }
                  ]
                }
              ]
            }
            """);

        var release = Assert.Single(releases);
        Assert.Equal("1.2", release.Version);
        Assert.Equal("2026-01-02", release.Date);
        Assert.Equal("Titel", release.Title);
        Assert.Equal(
            new[] { ChangelogChangeKind.Feature, ChangelogChangeKind.Fix, ChangelogChangeKind.Improvement },
            release.Changes.Select(change => change.Kind).ToArray());
    }

    [Fact]
    public void Parse_ReadsABareArrayOfReleases()
    {
        var releases = ChangelogCatalog.Parse("""[ { "version": "2.0" } ]""");

        Assert.Equal("2.0", Assert.Single(releases).Version);
    }

    [Fact]
    public void Parse_SortsNewestFirst()
    {
        var releases = ChangelogCatalog.Parse(
            """[ { "version": "0.3" }, { "version": "0.10" }, { "version": "0.4.0" } ]""");

        Assert.Equal(
            new[] { "0.10", "0.4.0", "0.3" },
            releases.Select(release => release.Version).ToArray());
    }

    [Fact]
    public void Parse_AcceptsChangesAsPlainStrings()
    {
        var releases = ChangelogCatalog.Parse("""[ { "version": "1.0", "changes": [ "Nur Text" ] } ]""");

        var change = Assert.Single(Assert.Single(releases).Changes);
        Assert.Equal("Nur Text", change.Text);
        Assert.Equal(ChangelogChangeKind.Improvement, change.Kind);
    }

    [Theory]
    [InlineData("feature", ChangelogChangeKind.Feature)]
    [InlineData("Neu", ChangelogChangeKind.Feature)]
    [InlineData("NEW", ChangelogChangeKind.Feature)]
    [InlineData("fix", ChangelogChangeKind.Fix)]
    [InlineData("behoben", ChangelogChangeKind.Fix)]
    [InlineData("Improvement", ChangelogChangeKind.Improvement)]
    [InlineData("voellig-unbekannt", ChangelogChangeKind.Improvement)]
    public void Parse_MapsTheKindSpellings(string kind, ChangelogChangeKind expected)
    {
        var releases = ChangelogCatalog.Parse(
            $$"""[ { "version": "1.0", "changes": [ { "kind": "{{kind}}", "text": "x" } ] } ]""");

        Assert.Equal(expected, Assert.Single(Assert.Single(releases).Changes).Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{ nicht wirklich json")]
    [InlineData("42")]
    [InlineData("{}")]
    public void Parse_ReturnsNothingForUnusableInput(string? json)
    {
        Assert.Empty(ChangelogCatalog.Parse(json));
    }

    [Fact]
    public void Parse_SkipsEntriesWithoutAVersionAndChangesWithoutText()
    {
        var releases = ChangelogCatalog.Parse(
            """
            [
              { "title": "ohne Version" },
              { "version": "  " },
              { "version": "1.0", "changes": [ "", { "kind": "fix" }, { "kind": "fix", "text": "gilt" } ] }
            ]
            """);

        var release = Assert.Single(releases);
        Assert.Equal("gilt", Assert.Single(release.Changes).Text);
    }
}

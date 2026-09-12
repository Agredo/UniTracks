using UniTracks.Services.Changelog;
using UniTracks.Tests.Changelog.Fakes;
using UniTracks.ViewModels.Controls.Popups;

namespace UniTracks.Tests.Changelog;

/// <summary>
/// Covers the content the "what's new" popup renders: the release of the running version plus the
/// ones before it.
/// </summary>
public sealed class WhatsNewPopupViewModelTests
{
    private readonly FakeChangelogService changelog = new();

    public WhatsNewPopupViewModelTests()
    {
        changelog.Items.AddRange(
        [
            new ChangelogRelease
            {
                Version = "0.5",
                Date = "2026-09-13",
                Title = "Laufrichtung",
                Changes = [new ChangelogChange { Kind = ChangelogChangeKind.Feature, Text = "Pfeile" }],
            },
            new ChangelogRelease
            {
                Version = "0.4",
                Date = "2026-09-12",
                Title = "Stabilität",
                Changes = [new ChangelogChange { Kind = ChangelogChangeKind.Fix, Text = "Kein Absturz" }],
            },
            new ChangelogRelease { Version = "0.3", Date = "2026-09-07", Title = "Statistiken" },
            new ChangelogRelease { Version = "0.2", Title = "Karte" },
            new ChangelogRelease { Version = "0.1", Title = "Erste Fassung" },
        ]);
    }

    [Fact]
    public void ShowsTheReleaseOfTheRunningVersion()
    {
        var viewModel = new WhatsNewPopupViewModel(changelog, "0.4");

        Assert.Equal("0.4", viewModel.Current.Version);
        Assert.Equal("NEU IN 0.4", viewModel.CurrentHeading);
        Assert.Equal("Version 0.4 · 12.09.2026", viewModel.Subtitle);
        Assert.Equal("Version 0.4 – Stabilität", viewModel.Current.Heading);
        Assert.Equal("BEHOBEN", Assert.Single(viewModel.Current.Changes).Label);
    }

    [Fact]
    public void FallsBackToTheNewestReleaseForAnUnknownVersion()
    {
        var viewModel = new WhatsNewPopupViewModel(changelog, "9.9");

        Assert.Equal("0.5", viewModel.Current.Version);
    }

    [Fact]
    public void ShowsTheThreeReleasesBeforeTheCurrentOne()
    {
        var viewModel = new WhatsNewPopupViewModel(changelog, "0.5");

        Assert.Equal(new[] { "0.4", "0.3", "0.2" }, viewModel.Earlier.Select(release => release.Version).ToArray());
        Assert.True(viewModel.HasEarlier);
    }

    [Fact]
    public void HasNoEarlierSectionForTheOldestRelease()
    {
        var viewModel = new WhatsNewPopupViewModel(changelog, "0.1");

        Assert.Empty(viewModel.Earlier);
        Assert.False(viewModel.HasEarlier);
    }

    [Fact]
    public void SurvivesAnEmptyChangelog()
    {
        changelog.Items.Clear();

        var viewModel = new WhatsNewPopupViewModel(changelog, "0.5");

        Assert.Equal("0.5", viewModel.Current.Version);
        Assert.Empty(viewModel.Current.Changes);
        Assert.False(viewModel.HasEarlier);
    }

    [Fact]
    public void ClosingReportsAConfirmedResult()
    {
        var viewModel = new WhatsNewPopupViewModel(changelog, "0.5");
        var results = new List<bool>();
        viewModel.Completed += (_, result) => results.Add(result);

        viewModel.CloseCommand.Execute(null);

        Assert.True(Assert.Single(results));
    }

    [Theory]
    [InlineData(ChangelogChangeKind.Feature, "feature", "NEU")]
    [InlineData(ChangelogChangeKind.Improvement, "improvement", "GEÄNDERT")]
    [InlineData(ChangelogChangeKind.Fix, "fix", "BEHOBEN")]
    public void MapsTheChangeKindToTheBadge(ChangelogChangeKind kind, string expectedKind, string expectedLabel)
    {
        var viewModel = new ChangelogChangeViewModel(new ChangelogChange { Kind = kind, Text = "x" });

        Assert.Equal(expectedKind, viewModel.Kind);
        Assert.Equal(expectedLabel, viewModel.Label);
    }
}

using UniTracks.Services.Changelog;
using UniTracks.Tests.Changelog.Fakes;
using UniTracks.Tests.ViewModels.Fakes;
using UniTracks.ViewModels.Changelog;

namespace UniTracks.Tests.Changelog;

/// <summary>
/// The notes are shown exactly once per installed version, so the decision has to be pinned down: a
/// fresh install shows them, a restart with the same version does not, an update does again.
/// </summary>
public sealed class ChangelogPresenterTests
{
    private const string CurrentVersion = "0.5";

    private readonly FakeChangelogService changelog = new();
    private readonly FakeChangelogState state = new();
    private readonly FakePopupNavigationService popups = new();

    public ChangelogPresenterTests()
    {
        changelog.Items.AddRange(
        [
            new ChangelogRelease { Version = "0.5", Title = "Neu" },
            new ChangelogRelease { Version = "0.4", Title = "Davor" },
        ]);
    }

    private ChangelogPresenter CreatePresenter() => new(changelog, state, popups, CurrentVersion);

    [Fact]
    public async Task ShowsTheNotesOnAFreshInstallAndRecordsTheVersion()
    {
        Assert.Null(state.LastSeenVersion);

        await CreatePresenter().ShowIfUnseenAsync();

        Assert.Equal(["WhatsNewPopupViewModel"], popups.ShownPopups);
        Assert.Equal(CurrentVersion, state.LastSeenVersion);
    }

    [Fact]
    public async Task ShowsTheNotesAgainAfterAnUpdate()
    {
        state.LastSeenVersion = "0.4";

        await CreatePresenter().ShowIfUnseenAsync();

        Assert.Single(popups.ShownPopups);
        Assert.Equal(CurrentVersion, state.LastSeenVersion);
    }

    [Theory]
    [InlineData("0.5")]
    [InlineData("0.5.0")]
    [InlineData("0.5.0.0")]
    public async Task DoesNotShowTheNotesTwiceForTheSameVersion(string lastSeen)
    {
        state.LastSeenVersion = lastSeen;

        await CreatePresenter().ShowIfUnseenAsync();

        Assert.Empty(popups.ShownPopups);
        Assert.Equal(lastSeen, state.LastSeenVersion);
    }

    [Fact]
    public async Task DoesNothingWhenTheChangelogIsEmpty()
    {
        changelog.Items.Clear();
        state.LastSeenVersion = "0.4";

        await CreatePresenter().ShowIfUnseenAsync();

        Assert.Empty(popups.ShownPopups);

        // Nothing was shown, so nothing may be marked as seen - otherwise the notes would be lost.
        Assert.Equal("0.4", state.LastSeenVersion);
    }

    [Fact]
    public async Task RetriesWhenThePopupCouldNotBePresented()
    {
        popups.ShowFailure = new InvalidOperationException("kein Fenster");

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreatePresenter().ShowIfUnseenAsync());

        Assert.Null(state.LastSeenVersion);

        popups.ShowFailure = null;
        await CreatePresenter().ShowIfUnseenAsync();

        Assert.Single(popups.ShownPopups);
        Assert.Equal(CurrentVersion, state.LastSeenVersion);
    }

    [Fact]
    public async Task DoesNothingWhenTheChangelogHasNoMatchingReleaseForTheVersion()
    {
        // A version that was bumped without a matching entry must not swallow the popup silently
        // for good; the state stays untouched so the next start can still show the notes.
        state.LastSeenVersion = null;

        await new ChangelogPresenter(changelog, state, popups, "9.9").ShowIfUnseenAsync();

        Assert.Single(popups.ShownPopups);
        Assert.Equal("9.9", state.LastSeenVersion);
    }
}

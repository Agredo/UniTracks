using AgredoApplication.MVVM.Services.Abstractions.Navigation;
using UniTracks.Services.Changelog;
using UniTracks.ViewModels.Controls.Popups;

namespace UniTracks.ViewModels.Changelog;

/// <summary>Presents the "what's new" notes after the app has been updated.</summary>
public interface IChangelogPresenter
{
    /// <summary>
    /// Shows the release notes when the running version has not been shown yet. Does nothing
    /// otherwise, so it is safe to call on every start.
    /// </summary>
    Task ShowIfUnseenAsync();

    /// <summary>
    /// Shows the release notes of the running version again, whether they were seen before or not.
    /// Used by the button in the settings that opens "Was ist neu".
    /// </summary>
    Task ShowAsync();
}

/// <inheritdoc />
public sealed class ChangelogPresenter : IChangelogPresenter
{
    private readonly IChangelogService changelog;
    private readonly IChangelogState state;
    private readonly IPopupNavigationService popupNavigation;
    private readonly string currentVersion;

    public ChangelogPresenter(
        IChangelogService changelog,
        IChangelogState state,
        IPopupNavigationService popupNavigation,
        string currentVersion)
    {
        this.changelog = changelog;
        this.state = state;
        this.popupNavigation = popupNavigation;
        this.currentVersion = currentVersion;
    }

    public async Task ShowIfUnseenAsync()
    {
        if (changelog.Releases.Count == 0)
        {
            return;
        }

        if (ChangelogVersion.IsSame(state.LastSeenVersion, currentVersion))
        {
            return;
        }

        await ShowAsync();
    }

    public async Task ShowAsync()
    {
        if (changelog.Releases.Count == 0)
        {
            return;
        }

        await popupNavigation.ShowPopupAsync<WhatsNewPopupViewModel>();

        // Recorded only after the notes were actually presented: when showing them fails, the next
        // start simply tries again instead of silently swallowing the release notes.
        state.LastSeenVersion = currentVersion;
    }
}

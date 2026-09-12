using AgredoApplication.MVVM.Services.Abstractions.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Services.Changelog;

namespace UniTracks.ViewModels.Controls.Popups;

/// <summary>
/// Shows what changed in the version that was just installed, together with the releases that came
/// before it. The content comes from <c>Changelog/changelog.json</c>, which is maintained by hand.
/// </summary>
public partial class WhatsNewPopupViewModel : ObservableObject, IPopupResultProvider<bool>
{
    /// <summary>How many older releases are listed below the current one.</summary>
    private const int MaxEarlierReleases = 3;

    public event EventHandler<bool>? Completed;

    public WhatsNewPopupViewModel(IChangelogService changelog, string currentVersion)
    {
        var current = changelog.Find(currentVersion) ?? changelog.Releases.FirstOrDefault();

        Current = new ChangelogReleaseViewModel(current ?? new ChangelogRelease { Version = currentVersion });
        CurrentHeading = $"NEU IN {Current.Version}";
        Subtitle = string.IsNullOrWhiteSpace(Current.Date)
            ? $"Version {Current.Version}"
            : $"Version {Current.Version} · {Current.Date}";

        Earlier =
        [
            .. changelog.Releases
                .Where(release => !ReferenceEquals(release, current))
                .Where(release => ChangelogVersion.Compare(release.Version, Current.Version) < 0)
                .Take(MaxEarlierReleases)
                .Select(release => new ChangelogReleaseViewModel(release)),
        ];
    }

    public ChangelogReleaseViewModel Current { get; }

    public IReadOnlyList<ChangelogReleaseViewModel> Earlier { get; }

    public string CurrentHeading { get; }

    public string Subtitle { get; }

    public bool HasEarlier => Earlier.Count > 0;

    [RelayCommand]
    private void Close() => Completed?.Invoke(this, true);
}

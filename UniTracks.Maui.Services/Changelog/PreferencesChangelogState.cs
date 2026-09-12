using UniTracks.Services.Changelog;

namespace UniTracks.Maui.Services.Changelog;

/// <inheritdoc />
public sealed class PreferencesChangelogState : IChangelogState
{
    private const string LastSeenVersionKey = "UniTracks.Changelog.LastSeenVersion";

    public string? LastSeenVersion
    {
        get
        {
            var stored = Preferences.Default.Get<string?>(LastSeenVersionKey, null);
            return string.IsNullOrWhiteSpace(stored) ? null : stored;
        }

        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Preferences.Default.Remove(LastSeenVersionKey);
                return;
            }

            Preferences.Default.Set(LastSeenVersionKey, value);
        }
    }
}

using UniTracks.Services.Settings;

namespace UniTracks.Maui.Services.Settings;

/// <inheritdoc />
public sealed class PreferencesTrackSmoothingSettings : ITrackSmoothingSettings
{
    private const string Key = "UniTracks.Settings.TrackSmoothingEnabled";

    /// <summary>On by default: the smoothing was the behaviour before the switch existed.</summary>
    public bool IsEnabled
    {
        get => Preferences.Default.Get(Key, true);
        set => Preferences.Default.Set(Key, value);
    }
}

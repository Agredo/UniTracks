using UniTracks.Services.Settings;

namespace UniTracks.Maui.Services.Settings;

/// <inheritdoc />
public sealed class PreferencesTripCardLayoutSettings : ITripCardLayoutSettings
{
    private const string Key = "UniTracks.Settings.TripCardsCompact";

    /// <summary>Off by default: the full card layout was the behaviour before the switch existed.</summary>
    public bool IsCompact
    {
        get => Preferences.Default.Get(Key, false);
        set => Preferences.Default.Set(Key, value);
    }
}

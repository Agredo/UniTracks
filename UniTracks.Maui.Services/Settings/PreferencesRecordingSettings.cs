using UniTracks.Services.Settings;

namespace UniTracks.Maui.Services.Settings;

/// <inheritdoc />
public sealed class PreferencesRecordingSettings : IRecordingSettings
{
    private const string LastTripTypeIdKey = "UniTracks.Settings.Recording.LastTripTypeId";

    public string LastTripTypeId
    {
        get => Preferences.Default.Get(LastTripTypeIdKey, string.Empty);
        set => Preferences.Default.Set(LastTripTypeIdKey, value);
    }
}

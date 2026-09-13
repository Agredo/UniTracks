using UniTracks.Services.Settings;

namespace UniTracks.Maui.Services.Settings;

/// <inheritdoc />
public sealed class PreferencesMapStyleSettings : IMapStyleSettings
{
    private const string Key = "UniTracks.Settings.MapStyle";

    /// <summary>OpenStreetMap standard, the style the app used before the picker existed.</summary>
    public MapStyleKind Style
    {
        get => MapStyleCatalog.Parse(Preferences.Default.Get<string?>(Key, null));
        set => Preferences.Default.Set(Key, MapStyleCatalog.ToStorageValue(value));
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Services.ApplicationModel;
using UniTracks.Services.ApplicationModel.Permissions;
using UniTracks.Services.Settings;

namespace UniTracks.ViewModels.Pages;

/// <summary>
/// Einstellungen der App. Neben der GPS-Glättung (sie wirkt ausschließlich auf die auf der Karte
/// gezeichnete Strecke — Distanzen und Statistiken bleiben immer geglättet) steht hier der Zustand
/// der Hintergrund-Standortfreigabe. Android 11+ und iOS bieten "Immer erlauben" nicht mehr im
/// normalen Dialog an, deshalb führt der Knopf notfalls auf die Systemseite der App.
/// </summary>
public partial class SettingsPageViewModel : ObservableObject
{
    public string SmoothingHint { get; } =
        "Aus zeigt die Karte die rohen GPS-Punkte inklusive Zickzack und Ausreißern. " +
        "An zeigt die gefilterte und gemittelte Strecke. " +
        "Distanzen und Statistiken werden immer aus der geglätteten Strecke berechnet.";

    public string SmoothingStateText => TrackSmoothingEnabled
        ? "Glättung aktiv"
        : "Glättung aus – Karte zeigt Rohdaten";

    public string BackgroundLocationHint { get; } =
        "Ohne \"Immer erlauben\" kann die Aufzeichnung enden, sobald die App in den Hintergrund " +
        "geht. Android 11+ und iOS zeigen die Option nur in den Systemeinstellungen bzw. im Dialog.";

    /// <summary>
    /// iOS und Android vergeben Vordergrund- und Hintergrundfreigabe getrennt. Tests setzen das für
    /// den Android/iOS-Zweig, die App nutzt die echte Plattform.
    /// </summary>
    public bool HasSeparateBackgroundLocationGrant { get; init; } =
        OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

    public bool IsBackgroundLocationVisible => HasSeparateBackgroundLocationGrant;

    public string BackgroundLocationStateText => backgroundLocationStatus switch
    {
        PermissionStatus.Granted => "Immer erlauben – freigegeben",
        PermissionStatus.Denied => "Nur beim Verwenden der App (oder nicht erlaubt)",
        PermissionStatus.Disabled => "Standort ist systemweit aus",
        _ => "Freigabe unbekannt"
    };

    public bool BackgroundLocationMissing => backgroundLocationStatus is not PermissionStatus.Granted;

    private readonly ITrackSmoothingSettings smoothingSettings;
    private readonly IPermissions permissions;
    private readonly IAppSettings appSettings;

    private PermissionStatus backgroundLocationStatus = PermissionStatus.Unknown;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SmoothingStateText))]
    private bool trackSmoothingEnabled;

    public SettingsPageViewModel(
        ITrackSmoothingSettings smoothingSettings,
        IPermissions permissions,
        IAppSettings appSettings)
    {
        this.smoothingSettings = smoothingSettings;
        this.permissions = permissions;
        this.appSettings = appSettings;

        trackSmoothingEnabled = smoothingSettings.IsEnabled;
    }

    /// <summary>
    /// Re-reads the permission on every appearance: the user may have changed it on the system page
    /// in between, and the value must never be cached by the persistent view model.
    /// </summary>
    public async Task RefreshBackgroundLocationAsync()
    {
        if (!IsBackgroundLocationVisible)
        {
            return;
        }

        backgroundLocationStatus = await permissions.CheckPermissionStatusAsync(Permission.LocationAlways);
        OnPropertyChanged(nameof(BackgroundLocationStateText));
        OnPropertyChanged(nameof(BackgroundLocationMissing));
    }

    /// <summary>
    /// Asks the system once and, when that does not grant background access (Android 11+ answers
    /// without any dialog), opens the app's settings page so the user can switch to "Immer erlauben".
    /// </summary>
    [RelayCommand]
    private async Task OpenBackgroundLocationSettingsAsync()
    {
        PermissionStatus status = await permissions.CheckPermissionStatusAsync(Permission.LocationAlways);

        if (status is not PermissionStatus.Granted)
        {
            status = await permissions.RequestPermissionAsync(Permission.LocationAlways);
        }

        if (status is not PermissionStatus.Granted)
        {
            // Falls der Dialog die Freigabe nicht erteilen kann, bleibt nur die Systemseite.
            await appSettings.ShowAsync();
        }

        await RefreshBackgroundLocationAsync();
    }

    /// <summary>Persists every flip of the switch; the map picks it up on its next appearance.</summary>
    partial void OnTrackSmoothingEnabledChanged(bool value)
    {
        smoothingSettings.IsEnabled = value;
    }
}

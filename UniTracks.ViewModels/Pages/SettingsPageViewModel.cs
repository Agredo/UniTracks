using AgredoApplication.MVVM.Services.Abstractions.IO;
using AgredoApplication.MVVM.Services.Abstractions.Navigation;
using AgredoApplication.MVVM.Services.Abstractions.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Services.ApplicationModel;
using UniTracks.Services.ApplicationModel.Permissions;
using UniTracks.Services.Data;
using UniTracks.Services.Settings;

namespace UniTracks.ViewModels.Pages;

/// <summary>
/// Alle Einstellungen der App an einer Stelle: Karte (GPS-Glättung und Kartenstil), Standort im
/// Hintergrund, die Datenbank (teilen, importieren, zurücksetzen) und die Seiten rund um die App.
///
/// Die Glättung wirkt ausschließlich auf die auf der Karte gezeichnete Strecke — Distanzen und
/// Statistiken bleiben immer geglättet. Android 11+ und iOS bieten "Immer erlauben" nicht mehr im
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

    /// <summary>Auswahl für den Kartenstil; dieselbe Liste, die der Katalog für die Karte nutzt.</summary>
    public IReadOnlyList<MapStyleOption> MapStyleOptions => MapStyleCatalog.Options;

    public string MapStyleDescription => SelectedMapStyle.Description;

    public string MapStyleAttributionHint => MapStyleCatalog.AttributionHint(SelectedMapStyle.Kind);

    /// <summary>Hinweis zum Import: Dateityp der laufenden Plattform und der Ablauf des Tauschs.</summary>
    public string ImportHint =>
        "Wähle die Datenbankdatei einer anderen UniTracks-Installation aus. " +
        "Der Import wird beim nächsten Start der App übernommen; deine jetzige Datenbank bleibt als " +
        "Kopie erhalten.";

    public string DatabaseInfoText =>
        $"{databaseMaintenance.DatabaseFileName} · {FormatSize(databaseMaintenance.DatabaseSizeBytes)}";

    public string DatabasePathText => databaseMaintenance.DatabasePath;

    /// <summary>Text der vorgemerkten Datenoperation; leer, wenn keine wartet.</summary>
    public string PendingDatabaseOperationText => databaseMaintenance.PendingOperationText;

    public bool HasPendingDatabaseOperation => databaseMaintenance.HasPendingOperation;

    public string LastImportText => databaseMaintenance.LastImport is { } timestamp
        ? $"Letzter Import: {timestamp.LocalDateTime:dd.MM.yyyy HH:mm}"
        : string.Empty;

    public bool HasLastImport => databaseMaintenance.LastImport is not null;

    private readonly ITrackSmoothingSettings smoothingSettings;
    private readonly IPermissions permissions;
    private readonly IAppSettings appSettings;
    private readonly IMapStyleSettings mapStyleSettings;
    private readonly IDatabaseMaintenance databaseMaintenance;
    private readonly IFileSystem fileSystem;
    private readonly IDialogService dialogService;

    private PermissionStatus backgroundLocationStatus = PermissionStatus.Unknown;

    /// <summary>Navigation der Seite, damit die Ansicht keine eigenen Befehle braucht.</summary>
    public INavigationService Navigation { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SmoothingStateText))]
    private bool trackSmoothingEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MapStyleDescription))]
    [NotifyPropertyChangedFor(nameof(MapStyleAttributionHint))]
    private MapStyleOption selectedMapStyle;

    /// <summary>Sperrt die Daten-Knöpfe, solange eine Operation läuft.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool isBusy;

    public bool IsNotBusy => !IsBusy;

    public SettingsPageViewModel(
        ITrackSmoothingSettings smoothingSettings,
        IPermissions permissions,
        IAppSettings appSettings,
        IMapStyleSettings mapStyleSettings,
        IDatabaseMaintenance databaseMaintenance,
        IFileSystem fileSystem,
        INavigationService navigation,
        IDialogService dialogService)
    {
        this.smoothingSettings = smoothingSettings;
        this.permissions = permissions;
        this.appSettings = appSettings;
        this.mapStyleSettings = mapStyleSettings;
        this.databaseMaintenance = databaseMaintenance;
        this.fileSystem = fileSystem;
        this.dialogService = dialogService;

        Navigation = navigation;

        trackSmoothingEnabled = smoothingSettings.IsEnabled;
        selectedMapStyle = MapStyleCatalog.Option(mapStyleSettings.Style);
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
    /// Re-reads the data section on every appearance: size, pending operation and last import are
    /// plain file lookups and therefore not observable on their own.
    /// </summary>
    public void RefreshDatabaseStatus()
    {
        OnPropertyChanged(nameof(DatabaseInfoText));
        OnPropertyChanged(nameof(DatabasePathText));
        OnPropertyChanged(nameof(PendingDatabaseOperationText));
        OnPropertyChanged(nameof(HasPendingDatabaseOperation));
        OnPropertyChanged(nameof(LastImportText));
        OnPropertyChanged(nameof(HasLastImport));
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

    /// <summary>Teilt eine Kopie der Datenbank; die laufende Datei bleibt dabei unangetastet.</summary>
    [RelayCommand]
    private async Task ShareDatabaseAsync()
    {
        IsBusy = true;

        try
        {
            var copy = await databaseMaintenance.CreateExportCopyAsync();

            if (copy is null)
            {
                await dialogService.AlertAsync(
                    "Datenbank teilen",
                    "Es gibt noch keine Datenbank zum Teilen. Starte einen ersten Lauf, danach ist die Datei vorhanden.",
                    "OK");
                return;
            }

            await fileSystem.ShareFilesAsync("UniTracks Datenbank", [copy]);
        }
        catch (Exception exception)
        {
            await dialogService.AlertAsync("Datenbank teilen", $"Das Teilen ist fehlgeschlagen: {exception.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
            RefreshDatabaseStatus();
        }
    }

    /// <summary>
    /// Wählt eine Datenbankdatei aus und merkt sie als Import vor. Der Tausch passiert beim nächsten
    /// Start der App, weil das laufende Repository die aktuelle Datei offen hält.
    /// </summary>
    [RelayCommand]
    private async Task ImportDatabaseAsync()
    {
        var path = await fileSystem.PickFilePath();

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        IsBusy = true;

        try
        {
            var error = await databaseMaintenance.StageImportAsync(path);

            if (error is not null)
            {
                await dialogService.AlertAsync("Datenbank importieren", error, "OK");
                return;
            }

            await dialogService.AlertAsync(
                "Import vorgemerkt",
                "Die Datei ist geprüft und wird beim nächsten Start der App übernommen.\n\n" +
                "Beende die App jetzt vollständig (im App-Umschalter nach oben wischen) und starte sie neu. " +
                "Deine bisherige Datenbank bleibt als Kopie im App-Ordner erhalten.",
                "OK");
        }
        finally
        {
            IsBusy = false;
            RefreshDatabaseStatus();
        }
    }

    /// <summary>Merkt das Löschen der Datenbank vor; eine datierte Kopie bleibt zur Sicherheit liegen.</summary>
    [RelayCommand]
    private async Task ResetDatabaseAsync()
    {
        var confirmed = await dialogService.ConfirmAsync(
            "Alle Daten löschen",
            "Alle Trips, Statistiken, Erfolge und Profildaten werden gelöscht; beim nächsten Start " +
            "beginnt UniTracks mit einer leeren Datenbank. Eine Kopie der jetzigen Datenbank bleibt " +
            "im App-Ordner erhalten.\n\nWirklich löschen?",
            "Löschen",
            "Abbrechen");

        if (!confirmed)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var error = await databaseMaintenance.StageResetAsync();

            if (error is not null)
            {
                await dialogService.AlertAsync("Daten löschen", error, "OK");
                return;
            }

            await dialogService.AlertAsync(
                "Löschen vorgemerkt",
                "Beende die App jetzt vollständig (im App-Umschalter nach oben wischen) und starte sie neu. " +
                "Danach ist die Datenbank leer.",
                "OK");
        }
        finally
        {
            IsBusy = false;
            RefreshDatabaseStatus();
        }
    }

    [RelayCommand]
    private async Task OpenProfileAsync()
    {
        await Navigation.ShellNavigationTo("ProfilePage", new Dictionary<string, object>());
    }

    [RelayCommand]
    private async Task OpenAboutAsync()
    {
        await Navigation.ShellNavigationTo("AboutPage", new Dictionary<string, object>());
    }

    [RelayCommand]
    private async Task OpenHelpAsync()
    {
        await Navigation.ShellNavigationTo("HelpPage", new Dictionary<string, object>());
    }

    [RelayCommand]
    private async Task OpenFeedbackAsync()
    {
        await Navigation.ShellNavigationTo("FeedbackPage", new Dictionary<string, object>());
    }

    /// <summary>Persists every flip of the switch; the map picks it up on its next appearance.</summary>
    partial void OnTrackSmoothingEnabledChanged(bool value)
    {
        smoothingSettings.IsEnabled = value;
    }

    /// <summary>
    /// Persists the picked style. The picker hands out the option object and can clear it while the
    /// list is bound, so null is ignored instead of being written to the settings.
    /// </summary>
    partial void OnSelectedMapStyleChanged(MapStyleOption value)
    {
        if (value is null)
        {
            return;
        }

        mapStyleSettings.Style = value.Kind;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        <= 0 => "noch keine Datei",
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.0} KB",
        _ => $"{bytes / (1024.0 * 1024.0):0.0} MB"
    };
}

using CommunityToolkit.Mvvm.ComponentModel;
using UniTracks.Services.Settings;

namespace UniTracks.ViewModels.Pages;

/// <summary>
/// Einstellungen der App. Aktuell nur die GPS-Glättung: Sie wirkt ausschließlich auf die auf der
/// Karte gezeichnete Strecke (Distanzen und Statistiken bleiben immer geglättet), damit sich
/// nachvollziehen lässt, was die Glättung verschluckt — z. B. enge Kehren einer 100-m-Strecke
/// hin und zurück.
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

    private readonly ITrackSmoothingSettings smoothingSettings;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SmoothingStateText))]
    private bool trackSmoothingEnabled;

    public SettingsPageViewModel(ITrackSmoothingSettings smoothingSettings)
    {
        this.smoothingSettings = smoothingSettings;

        trackSmoothingEnabled = smoothingSettings.IsEnabled;
    }

    /// <summary>Persists every flip of the switch; the map picks it up on its next appearance.</summary>
    partial void OnTrackSmoothingEnabledChanged(bool value)
    {
        smoothingSettings.IsEnabled = value;
    }
}

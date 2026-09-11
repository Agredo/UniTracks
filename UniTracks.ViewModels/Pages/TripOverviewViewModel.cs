using System.Collections.ObjectModel;
using System.Globalization;
using AgredoApplication.MVVM.Services.Abstractions.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Models.Trip;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.ViewModels.Pages;

public partial class TripOverviewViewModel : ObservableObject
{
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    public INavigationService Navigation { get; }

    [ObservableProperty]
    private Trip? trip;

    [ObservableProperty]
    private ObservableCollection<LocationModel> locations = new ObservableCollection<LocationModel>();

    [ObservableProperty]
    private string tripName = "Trip";

    [ObservableProperty]
    private string dateText = string.Empty;

    [ObservableProperty]
    private string distanceText = "-";

    [ObservableProperty]
    private string distanceUnit = "km";

    [ObservableProperty]
    private string durationText = "-";

    [ObservableProperty]
    private string averageSpeedText = "-";

    [ObservableProperty]
    private string maxSpeedText = "-";

    /// <summary>Both overlay cards are shown/hidden together by tapping the map.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowProfile))]
    private bool isOverlayVisible = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowProfile))]
    private bool hasProfile;

    /// <summary>The profile card only exists when the trip has enough GPS points to draw it.</summary>
    public bool ShowProfile => HasProfile && IsOverlayVisible;

    [ObservableProperty]
    private IList<double> speedProfile = new List<double>();

    [ObservableProperty]
    private IList<double> altitudeProfile = new List<double>();

    public TripOverviewViewModel(INavigationService navigation)
    {
        Navigation = navigation;

        Navigation.Parameters.TryGetValue("parameter", out var parameter);

        Trip = parameter as Trip;

        if (Trip is not null)
        {
            Trip.Locations?.ForEach(location => Locations.Add(location));
            ApplyTripStats(Trip);
        }
    }

    private void ApplyTripStats(Trip trip)
    {
        TripName = GetTripName(trip.StartTime);
        DateText = trip.StartTime.ToString("dddd, dd. MMMM yyyy · HH:mm", GermanCulture);

        if (trip.Distance is { } distance)
        {
            if (distance >= 1000)
            {
                DistanceText = (distance / 1000).ToString("0.00", GermanCulture);
                DistanceUnit = "km";
            }
            else
            {
                DistanceText = Math.Round(distance).ToString("0", GermanCulture);
                DistanceUnit = "m";
            }
        }

        TimeSpan duration = trip.EndTime - trip.StartTime;
        if (duration > TimeSpan.Zero)
        {
            DurationText = duration.TotalHours >= 1
                ? duration.ToString(@"h\:mm\:ss", GermanCulture)
                : duration.ToString(@"mm\:ss", GermanCulture);
        }

        if (trip.AverageSpeed is { } averageSpeed)
        {
            AverageSpeedText = Math.Round(averageSpeed * 3.6, 1).ToString("0.0", GermanCulture);
        }

        if (trip.MaxSpeed is { } maxSpeed)
        {
            MaxSpeedText = Math.Round(maxSpeed * 3.6, 1).ToString("0.0", GermanCulture);
        }

        ApplyLocationStats(trip);
    }

    /// <summary>
    /// The speed/altitude profile is not stored on the trip — it is derived from the
    /// recorded GPS points. The full set of derived metrics lives on the analysis page.
    /// </summary>
    private void ApplyLocationStats(Trip trip)
    {
        var locations = trip.Locations?
            .OrderBy(l => l.Timestamp)
            .ToList();

        if (locations is null || locations.Count < 2)
        {
            return;
        }

        BuildProfiles(locations);
    }

    /// <summary>Resamples the GPS points to at most 100 values so the charts stay cheap to draw.</summary>
    private void BuildProfiles(List<LocationModel> locations)
    {
        const int maxPoints = 100;
        int stride = Math.Max(1, locations.Count / maxPoints);

        var speeds = new List<double>();
        var altitudes = new List<double>();
        for (int i = 0; i < locations.Count; i += stride)
        {
            speeds.Add(locations[i].Speed * 3.6);
            altitudes.Add(locations[i].Altitude);
        }

        if (speeds.Count >= 2)
        {
            SpeedProfile = speeds;
            AltitudeProfile = altitudes;
            HasProfile = true;
        }
    }

    /// <summary>
    /// Tap on the map: hide both overlay cards so the route is visible, tap again to bring them back.
    /// </summary>
    [RelayCommand]
    private void ToggleOverlay() => IsOverlayVisible = !IsOverlayVisible;

    [RelayCommand]
    private async Task OpenDetails()
    {
        if (Trip is not null)
        {
            await Navigation.ShellNavigationTo("TripChartsPage", new Dictionary<string, object> { { "parameter", Trip } });
        }
    }

    private static string GetTripName(DateTimeOffset startTime)
    {
        return startTime.Hour switch
        {
            >= 5 and < 11 => "Morgen Trip",
            >= 11 and < 14 => "Mittags Trip",
            >= 14 and < 18 => "Nachmittags Trip",
            _ => "Abend Trip",
        };
    }
}

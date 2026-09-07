using System.Collections.ObjectModel;
using System.Globalization;
using AgredoApplication.MVVM.Services.Abstractions.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using UniTracks.Models.Trip;
using UniTracks.Services.Stats;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.ViewModels.Pages;

/// <summary>
/// Detail view for a single trip: all derived stats plus speed/altitude/pace
/// profiles rendered as full charts with labelled axes.
/// </summary>
public partial class TripChartsPageViewModel : ObservableObject
{
    private const int MaxProfilePoints = 200;
    private const int TimeLabelCount = 5;
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    public INavigationService Navigation { get; }

    [ObservableProperty]
    private Trip? trip;

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

    [ObservableProperty]
    private string movingTimeText = "-";

    [ObservableProperty]
    private string stoppedTimeText = "-";

    [ObservableProperty]
    private string altitudeText = "-";

    [ObservableProperty]
    private string paceText = "-";

    [ObservableProperty]
    private bool hasSpeedProfile;

    [ObservableProperty]
    private bool hasAltitudeProfile;

    [ObservableProperty]
    private bool hasPaceProfile;

    [ObservableProperty]
    private IList<double> speedProfile = new List<double>();

    [ObservableProperty]
    private IList<double> altitudeProfile = new List<double>();

    [ObservableProperty]
    private IList<double> paceProfile = new List<double>();

    [ObservableProperty]
    private IList<string> timeAxisLabels = new List<string>();

    public TripChartsPageViewModel(INavigationService navigation)
    {
        Navigation = navigation;

        Navigation.Parameters.TryGetValue("parameter", out var parameter);

        Trip = parameter as Trip;

        if (Trip is not null)
        {
            ApplyTripStats(Trip);
        }
    }

    private void ApplyTripStats(Trip trip)
    {
        TripName = trip.StartTime.Hour switch
        {
            >= 5 and < 11 => "Morgen Trip",
            >= 11 and < 14 => "Mittags Trip",
            >= 14 and < 18 => "Nachmittags Trip",
            _ => "Abend Trip",
        };
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
            DurationText = FormatDuration(duration);
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

    private void ApplyLocationStats(Trip trip)
    {
        var locations = trip.Locations?
            .OrderBy(l => l.Timestamp)
            .ToList();

        if (locations is null || locations.Count < 2)
        {
            return;
        }

        var (moving, stopped) = TripMetricsCalculator.ComputeMovingAndStopped(locations);
        MovingTimeText = FormatDuration(moving);
        StoppedTimeText = FormatDuration(stopped);

        double minAltitude = trip.MinAltitude ?? locations.Min(l => l.Altitude);
        double maxAltitude = trip.MaxAltitude ?? locations.Max(l => l.Altitude);
        AltitudeText = $"{Math.Round(minAltitude)}–{Math.Round(maxAltitude)} m";

        double distanceKm = (trip.Distance ?? 0) / 1000.0;
        if (distanceKm > 0.05 && moving.TotalSeconds > 0)
        {
            var pace = TimeSpan.FromSeconds(moving.TotalSeconds / distanceKm);
            PaceText = $"{(int)pace.TotalMinutes}:{pace.Seconds:00}";
        }

        BuildProfiles(locations);
    }

    /// <summary>
    /// Resamples the GPS points to at most <see cref="MaxProfilePoints"/> values and derives
    /// speed (km/h), altitude (m) and pace (min/km) series plus shared time axis labels.
    /// </summary>
    private void BuildProfiles(List<LocationModel> locations)
    {
        int stride = Math.Max(1, locations.Count / MaxProfilePoints);

        var sampled = new List<LocationModel>();
        for (int i = 0; i < locations.Count; i += stride)
        {
            sampled.Add(locations[i]);
        }

        if (sampled.Count < 2)
        {
            return;
        }

        SpeedProfile = sampled.Select(l => l.Speed * 3.6).ToList();
        HasSpeedProfile = true;

        AltitudeProfile = sampled.Select(l => l.Altitude).ToList();
        HasAltitudeProfile = AltitudeProfile.Max() - AltitudeProfile.Min() > 0.5;

        // Pace is only meaningful while actually moving — cap at 20 min/km so
        // standstill outliers don't flatten the whole chart.
        var pace = sampled
            .Select(l => l.Speed * 3.6 > 1.0 ? Math.Min(60.0 / (l.Speed * 3.6), 20.0) : 20.0)
            .ToList();
        HasPaceProfile = pace.Any(p => p < 19.9);
        PaceProfile = pace;

        TimeAxisLabels = BuildTimeAxisLabels(sampled);
    }

    private static IList<string> BuildTimeAxisLabels(List<LocationModel> sampled)
    {
        var start = sampled[0].Timestamp;
        var labels = new List<string>();
        for (int i = 0; i < TimeLabelCount; i++)
        {
            int index = (int)Math.Round((double)i * (sampled.Count - 1) / (TimeLabelCount - 1));
            var elapsed = sampled[index].Timestamp - start;
            labels.Add(elapsed.TotalHours >= 1
                ? elapsed.ToString(@"h\:mm\:ss")
                : elapsed.ToString(@"mm\:ss"));
        }

        return labels;
    }

    private static string FormatDuration(TimeSpan duration) =>
        duration.TotalHours >= 1
            ? duration.ToString(@"h\:mm\:ss", GermanCulture)
            : duration.ToString(@"mm\:ss", GermanCulture);
}

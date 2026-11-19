using System.Collections.ObjectModel;
using System.Globalization;
using AgredoApplication.MVVM.Services.Abstractions.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using UniTracks.Models.Trip;
using UniTracks.Services.Comparison;
using UniTracks.Services.Stats;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.ViewModels.Pages;

/// <summary>One line of the best-effort list: a distance, the time for it and the pace.</summary>
public sealed record BestEffortRow
{
    public required string DistanceText { get; init; }

    public required string TimeText { get; init; }

    public required string RateText { get; init; }
}

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
    private string paceLabel = "PACE /KM";

    [ObservableProperty]
    private string elevationGainText = "-";

    [ObservableProperty]
    private string paceChartTitle = "TEMPOVERLAUF";

    [ObservableProperty]
    private string paceAxisUnit = "min/km";

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

    [ObservableProperty]
    private bool hasBestEfforts;

    /// <summary>Fastest 1 km, 5 km and 10 km inside this trip, ascending by distance.</summary>
    public IReadOnlyList<BestEffortRow> BestEfforts { get; private set; } = Array.Empty<BestEffortRow>();

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
        TripName = TripDisplay.Name(trip);
        DateText = trip.StartTime.LocalDateTime.ToString("dddd, dd. MMMM yyyy · HH:mm", GermanCulture);

        var locations = trip.Locations?
            .OrderBy(location => location.Timestamp)
            .ToList() ?? new List<LocationModel>();
        double distanceMeters = TripMetrics.DistanceMeters(trip, locations);

        if (distanceMeters >= 1000)
        {
            DistanceText = (distanceMeters / 1000).ToString("0.00", GermanCulture);
            DistanceUnit = "km";
        }
        else
        {
            DistanceText = Math.Round(distanceMeters).ToString("0", GermanCulture);
            DistanceUnit = "m";
        }

        TimeSpan duration = TimeSpan.FromSeconds(TripMetrics.ElapsedSeconds(trip));
        if (duration > TimeSpan.Zero)
        {
            DurationText = FormatDuration(duration);
        }

        if (trip.AverageSpeed is { } averageSpeed && (trip.Locations is null || trip.Locations.Count < 2))
        {
            AverageSpeedText = Math.Round(averageSpeed * 3.6, 1).ToString("0.0", GermanCulture);
        }

        if (trip.MaxSpeed is { } maxSpeed)
        {
            MaxSpeedText = Math.Round(maxSpeed * 3.6, 1).ToString("0.0", GermanCulture);
        }

        PaceLabel = TripDisplay.RateLabel(trip.TripType);
        PaceChartTitle = TripDisplay.RateChartTitle(trip.TripType);
        PaceAxisUnit = TripDisplay.RateAxisUnit(trip.TripType);

        ApplyLocationStats(trip);
    }

    private void ApplyLocationStats(Trip trip)
    {
        var locations = trip.Locations?
            .OrderBy(l => l.Timestamp)
            .ToList() ?? new List<LocationModel>();

        if (locations.Count < 2)
        {
            return;
        }

        var (_, stopped) = TripMetricsCalculator.ComputeMovingAndStopped(locations);
        double movingSeconds = TripMetrics.MovingSeconds(trip, locations);
        MovingTimeText = FormatDuration(TimeSpan.FromSeconds(movingSeconds));
        StoppedTimeText = FormatDuration(TimeSpan.FromSeconds(
            Math.Max(0, trip.StoppedTime ?? stopped.TotalSeconds)));

        double minAltitude = trip.MinAltitude ?? locations.Min(l => l.Altitude);
        double maxAltitude = trip.MaxAltitude ?? locations.Max(l => l.Altitude);
        AltitudeText = $"{Math.Round(minAltitude)}–{Math.Round(maxAltitude)} m";
        double elevationGain = trip.ElevationGain
            ?? TripMetricsCalculator.ComputeElevationGain(locations);
        ElevationGainText = Math.Round(elevationGain).ToString("0", GermanCulture) + " m";

        double distanceMeters = TripMetrics.DistanceMeters(trip, locations);
        if (distanceMeters > 50 && movingSeconds > 0)
        {
            PaceText = TripDisplay.FormatRate(
                trip.TripType,
                TripDisplay.SecondsPerKilometer(distanceMeters, movingSeconds));
        }

        BuildProfiles(locations);
        ApplyBestEfforts(trip, locations);

        AverageSpeedText = Math.Round(
            TripMetrics.AverageSpeedMetersPerSecond(trip, locations) * 3.6, 1)
            .ToString("0.0", GermanCulture);
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
        if (TripDisplay.IsPace(Trip?.TripType))
        {
            double maximumSecondsPerKilometer =
                TripDisplay.RateKind(Trip?.TripType) == TripRateKind.PacePer100Meters
                    ? 100 * 60
                    : 60 * 60;
            var pace = sampled
                .Select(location =>
                {
                    double secondsPerKilometer = location.Speed > 0
                        ? Math.Min(1000.0 / location.Speed, maximumSecondsPerKilometer)
                        : maximumSecondsPerKilometer;
                    return TripDisplay.RateValue(Trip?.TripType, secondsPerKilometer);
                })
                .ToList();
            HasPaceProfile = pace.Any(value => value < TripDisplay.RateValue(
                Trip?.TripType,
                maximumSecondsPerKilometer));
            PaceProfile = pace;
        }
        else
        {
            HasPaceProfile = false;
            PaceProfile = new List<double>();
        }

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

    private void ApplyBestEfforts(Trip trip, List<LocationModel> locations)
    {
        BestEfforts = BestEffortCalculator.Compute(locations)
            .Select(effort => new BestEffortRow
            {
                DistanceText = FormatEffortDistance(effort.DistanceMeters),
                TimeText = FormatDuration(TimeSpan.FromSeconds(Math.Round(effort.Seconds))),
                RateText = TripDisplay.FormatRate(trip.TripType, effort.PaceSecondsPerKilometer),
            })
            .ToList();

        HasBestEfforts = BestEfforts.Count > 0;
        OnPropertyChanged(nameof(BestEfforts));
    }

    private static string FormatEffortDistance(double meters) =>
        meters >= 1000
            ? (meters / 1000).ToString("0", GermanCulture) + " km"
            : Math.Round(meters).ToString("0", GermanCulture) + " m";

    private static string FormatDuration(TimeSpan duration) =>
        duration.TotalHours >= 1
            ? duration.ToString(@"h\:mm\:ss", GermanCulture)
            : duration.ToString(@"mm\:ss", GermanCulture);
}

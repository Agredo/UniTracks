using System.Collections.ObjectModel;
using System.Globalization;
using AgredoApplication.MVVM.Services.Abstractions.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
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

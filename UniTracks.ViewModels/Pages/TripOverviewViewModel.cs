using System.Collections.ObjectModel;
using System.Globalization;
using AgredoApplication.MVVM.Services.Abstractions.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Data.Repository;
using UniTracks.Models.Trip;
using UniTracks.Services.Comparison;
using UniTracks.Services.Settings;
using UniTracks.ViewModels.Controls.Popups;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.ViewModels.Pages;

public partial class TripOverviewViewModel : ObservableObject
{
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    public INavigationService Navigation { get; }

    private readonly IPopupNavigationService popupNavigation;
    private readonly IRepository repository;
    private readonly ITripFingerprintService fingerprintService;
    private readonly ITrackSmoothingSettings smoothingSettings;
    private readonly IMapStyleSettings mapStyleSettings;

    /// <summary>Set once the track was read, so a re-appearing page does not query again.</summary>
    private bool trackLoaded;

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

    /// <summary>
    /// Drives whether <c>MapView</c> draws the smoothed track or the raw recorded points. Read from
    /// the settings on every appearance so a change made on the settings page shows up.
    /// </summary>
    [ObservableProperty]
    private bool isSmoothingEnabled = true;

    /// <summary>Tile layer of the map; also re-read on every appearance.</summary>
    [ObservableProperty]
    private MapStyleKind mapStyle = MapStyleCatalog.Default;

    public TripOverviewViewModel(
        INavigationService navigation,
        IPopupNavigationService popupNavigation,
        IRepository repository,
        ITripFingerprintService fingerprintService,
        ITrackSmoothingSettings smoothingSettings,
        IMapStyleSettings mapStyleSettings)
    {
        Navigation = navigation;
        this.popupNavigation = popupNavigation;
        this.repository = repository;
        this.fingerprintService = fingerprintService;
        this.smoothingSettings = smoothingSettings;
        this.mapStyleSettings = mapStyleSettings;

        IsSmoothingEnabled = smoothingSettings.IsEnabled;
        MapStyle = mapStyleSettings.Style;

        Navigation.Parameters.TryGetValue("parameter", out var parameter);

        Trip = parameter as Trip;

        if (Trip is not null)
        {
            Trip.Locations?.ForEach(location => Locations.Add(location));
            ApplyTripStats(Trip);
        }
    }

    /// <summary>
    /// Re-reads the smoothing switch. Called from the page's <c>OnAppearing</c> because a Shell tab
    /// switch keeps this page (and its map) alive, so the setting could have changed in between.
    /// </summary>
    public void RefreshSettings()
    {
        IsSmoothingEnabled = smoothingSettings.IsEnabled;
        MapStyle = mapStyleSettings.Style;
    }

    /// <summary>
    /// Reads the GPS points of the shown trip. The trip list hands the trip over without its points
    /// (reading them for every trip made that tab far too slow), so the one trip that is opened loads
    /// its own track — the map, the profile and the pages opened from here all work off those points.
    /// Called from the page's <c>OnAppearing</c>.
    /// </summary>
    public async Task LoadTrackAsync()
    {
        if (Trip is not { } trip || trackLoaded || trip.Locations is { Count: > 0 })
        {
            return;
        }

        trackLoaded = true;

        var points = (await repository.GetAsync<LocationModel>(location => location.TripID == trip.ID))
            .OrderBy(location => location.Timestamp)
            .ToList();

        if (points.Count == 0)
        {
            return;
        }

        // The map draws what its bound property holds, and a binding only reacts to a new value, so
        // the points go into a fresh collection instead of into the one already assigned.
        trip.Locations = points;
        Locations = new ObservableCollection<LocationModel>(points);
        ApplyLocationStats(trip);
    }

    private void ApplyTripStats(Trip trip)
    {
        TripName = trip.Name ?? GetTripName(trip.StartTime);
        DateText = trip.StartTime.LocalDateTime.ToString("dddd, dd. MMMM yyyy · HH:mm", GermanCulture);

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
            // The charts read the points off the trip they are handed, so make sure they are there
            // even when the user tapped before the track had finished loading.
            await LoadTrackAsync();
            await Navigation.ShellNavigationTo("TripChartsPage", new Dictionary<string, object> { { "parameter", Trip } });
        }
    }

    /// <summary>
    /// Opens the comparison entry page: how often this route was run and which other trips are worth
    /// comparing. Kept separate from the charts page so the analysis stays one tap away.
    /// </summary>
    [RelayCommand]
    private async Task Compare()
    {
        if (Trip is not null)
        {
            await Navigation.ShellNavigationTo("TripComparePage", new Dictionary<string, object> { { "parameter", Trip } });
        }
    }

    /// <summary>
    /// Opens the edit popup for name, type and note. A null result means the user cancelled.
    /// </summary>
    [RelayCommand]
    private async Task Edit()
    {
        if (Trip is null)
        {
            return;
        }

        var result = await popupNavigation.ShowPopupAsync<TripEditPopupViewModel, TripEditResult?>(
            viewModel => viewModel.InitializeAsync(Trip));

        if (result is not null)
        {
            await ApplyEditAsync(result);
        }
    }

    /// <summary>
    /// Persists the edit and refreshes the page. The fingerprint is only rebuilt when the type
    /// actually changed — name and note are not fingerprint inputs, and the rebuild touches the
    /// trip's GPS points.
    /// </summary>
    public async Task ApplyEditAsync(TripEditResult result)
    {
        if (Trip is null)
        {
            return;
        }

        Trip.Name = result.Name;
        Trip.Description = result.Description;

        bool typeChanged = result.TripType is not null && result.TripType.ID != Trip.TripTypeId;
        if (typeChanged)
        {
            Trip.TripType = result.TripType;
            Trip.TripTypeId = result.TripType!.ID;
        }

        await repository.Update(Trip);

        if (typeChanged)
        {
            await fingerprintService.RebuildAsync(Trip);
        }

        TripName = Trip.Name ?? GetTripName(Trip.StartTime);
    }

    private static string GetTripName(DateTimeOffset startTime)
    {
        return Services.Comparison.TripDisplay.TimeOfDayName(startTime);
    }
}

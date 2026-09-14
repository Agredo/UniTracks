using System.Collections.ObjectModel;
using AgredoApplication.MVVM.Services.Abstractions.IO;
using AgredoApplication.MVVM.Services.Abstractions.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Data.Repository;
using UniTracks.Models.Trip;
using UniTracks.Services.Comparison;
using UniTracks.Services.Data;
using UniTracks.Services.Location;
using UniTracks.Services.Settings;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.ViewModels.Pages.Tabs;

public partial class TripTabPageViewModel : ObservableObject
{
    /// <summary>
    /// Trips the list shows at once. Reaching the end of the visible block appends the next one, so a
    /// card only has to be built for what is actually on screen.
    /// </summary>
    public const int PageSize = 15;

    public INavigationService Navigation { get; }
    public IPopupNavigationService PopupNavigation { get; }
    public ILocationService LocationService { get; }
    public IFileSystem FileSystem { get; }
    public IGpsDataStorageService GpsDataStorageService { get; }
    public IRepository Repository { get; }
    public ITripCardLayoutSettings TripCardLayoutSettings { get; }
    public string DatabasePath { get; }

    private readonly ITripFingerprintService fingerprints;

    /// <summary>
    /// Every trip, newest first. <see cref="Trips"/> holds the leading block of this list, so the
    /// list itself stays the single source of the sort order while paging.
    /// </summary>
    private readonly List<Trip> allTrips = new List<Trip>();

    [ObservableProperty]
    private ObservableCollection<Trip> trips = new ObservableCollection<Trip>();

    /// <summary>True while further trips are waiting behind <see cref="LoadMoreTripsCommand"/>.</summary>
    [ObservableProperty]
    private bool hasMoreTrips;

    [ObservableProperty]
    private bool isCompactLayout;

    [ObservableProperty]
    private string? debugText;

    private Trip? selectedTrip;
    public Trip? SelectedTrip
    {
        get => selectedTrip;
        set
        {
            if (SetProperty(ref selectedTrip, value) && value is not null)
            {
                _ = Navigation.ShellNavigationTo("TripOverviewPage", new Dictionary<string, object> { { "parameter", value } });
            }
        }
    }

    [ObservableProperty]
    private bool refreshIndicatorVisible;

    public TripTabPageViewModel(
        INavigationService navigation,
        IPopupNavigationService popupNavigation,
        ILocationService locationService,
        IFileSystem fileSystem,
        IGpsDataStorageService gpsDataStorageService,
        IRepository repository,
        ITripFingerprintService fingerprints,
        ITripCardLayoutSettings tripCardLayoutSettings)
    {
        Navigation = navigation;
        PopupNavigation = popupNavigation;
        LocationService = locationService;
        FileSystem = fileSystem;
        GpsDataStorageService = gpsDataStorageService;
        Repository = repository;
        this.fingerprints = fingerprints;
        TripCardLayoutSettings = tripCardLayoutSettings;
        DatabasePath = repository.DatabasePath;
        IsCompactLayout = tripCardLayoutSettings.IsCompact;

        _ = GetTrips();
    }

    /// <summary>Re-reads the card layout preference (called from the page's OnAppearing).</summary>
    public void RefreshLayoutSettings()
    {
        IsCompactLayout = TripCardLayoutSettings.IsCompact;
    }

    private async Task GetTrips()
    {
        // Read without the GPS points on purpose: a trip carries one point per second of recording,
        // so pulling them in for every trip made this the most expensive read in the app - while the
        // cards only need the trip's own values and its type. The overview reads the points of the
        // one trip it opens.
        var orderedTrips = (await Repository.GetAllAsync<Trip>(
                trip => trip.TripType))
            .OrderByDescending(trip => trip.StartTime)
            .ToList();

        allTrips.Clear();
        allTrips.AddRange(orderedTrips);

        // Assigning the collection in one go raises a single change notification instead of one
        // per trip, which keeps the list from re-measuring itself for every single item.
        RefreshIndicatorVisible = false;
        Trips = new ObservableCollection<Trip>(allTrips.Take(PageSize));
        HasMoreTrips = allTrips.Count > Trips.Count;
    }

    /// <summary>
    /// Appends the next block of trips. The list calls this when the user scrolls close to the end
    /// (and from its footer, which covers a screen tall enough to show the whole first block).
    /// </summary>
    [RelayCommand]
    private void LoadMoreTrips()
    {
        if (!HasMoreTrips)
        {
            return;
        }

        foreach (var trip in allTrips.Skip(Trips.Count).Take(PageSize))
        {
            Trips.Add(trip);
        }

        HasMoreTrips = allTrips.Count > Trips.Count;
    }

    [RelayCommand]
    private void SelectedTripChanged()
    {
        // Selection is handled via the SelectedTrip property setter.
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await GetTrips();
        RefreshIndicatorVisible = false;
    }

    public async Task RenameTripAsync(Trip trip, string newName)
    {
        trip.Name = newName;
        await Repository.Update(trip);
        await GetTrips();
    }

    public async Task DeleteTripAsync(Trip trip)
    {
        // A trip owns its GPS points, but the store has no cascade delete for them (the foreign key
        // is only set to NULL), so remove the points explicitly — otherwise every deleted trip left
        // its locations behind and the database grew without bound.
        var locations = (await Repository.GetAsync<LocationModel>(location => location.TripID == trip.ID)).ToList();
        if (locations.Count > 0)
        {
            await Repository.DeleteRange(locations);
        }

        await Repository.Delete(trip);

        // The fingerprint is a derived summary of the trip, so it goes with it. Leaving it behind
        // would keep a deleted trip listed in every other trip's comparison.
        await fingerprints.RemoveAsync(trip.ID);

        await GetTrips();
    }
}

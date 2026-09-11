using System.Collections.ObjectModel;
using AgredoApplication.MVVM.Services.Abstractions.IO;
using AgredoApplication.MVVM.Services.Abstractions.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Data.Repository;
using UniTracks.Models.Trip;
using UniTracks.Services.Data;
using UniTracks.Services.Location;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.ViewModels.Pages.Tabs;

public partial class TripTabPageViewModel : ObservableObject
{
    public INavigationService Navigation { get; }
    public IPopupNavigationService PopupNavigation { get; }
    public ILocationService LocationService { get; }
    public IFileSystem FileSystem { get; }
    public IGpsDataStorageService GpsDataStorageService { get; }
    public IRepository Repository { get; }
    public string DatabasePath { get; }

    [ObservableProperty]
    private ObservableCollection<Trip> trips = new ObservableCollection<Trip>();

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
        IRepository repository)
    {
        Navigation = navigation;
        PopupNavigation = popupNavigation;
        LocationService = locationService;
        FileSystem = fileSystem;
        GpsDataStorageService = gpsDataStorageService;
        Repository = repository;
        DatabasePath = repository.DatabasePath;

        _ = GetTrips();
    }

    private async Task GetTrips()
    {
        var orderedTrips = (await Repository.GetAllAsync<Trip>(trip => trip.Locations))
            .OrderByDescending(trip => trip.StartTime)
            .ToList();

        // Assigning the collection in one go raises a single change notification instead of one
        // per trip, which keeps the list from re-measuring itself for every single item.
        RefreshIndicatorVisible = false;
        Trips = new ObservableCollection<Trip>(orderedTrips);
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
        await GetTrips();
    }
}

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
    private ObservableCollection<LocationModel> locations = new ObservableCollection<LocationModel>();

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
        Trips.Clear();
        var orderedTrips = (await Repository.GetAllAsync<Trip>(trip => trip.Locations))
            .OrderByDescending(trip => trip.StartTime)
            .ToList();

        foreach (var trip in orderedTrips)
        {
            Trips.Add(trip);
        }

        if (Trips.Count > 0)
        {
            Locations.Clear();
            Trip lastTrip = Trips.Last();

            Console.WriteLine($"Last Trip: {lastTrip.ID} {lastTrip.StartTime}");
            lastTrip.Locations?.ForEach(location => Locations.Add(location));
        }
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
        await Repository.Delete(trip);
        await GetTrips();
    }
}

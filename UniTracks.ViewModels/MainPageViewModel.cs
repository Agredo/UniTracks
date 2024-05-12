using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoCoordinatePortable;
using System.Collections.ObjectModel;
using UniTracks.Common.Contants;
using UniTracks.Common.ExtensionMethods;
using UniTracks.Data.LiteDB;
using UniTracks.Data.Repository;
using UniTracks.Data.SQLite;
using UniTracks.Models.Location;
using UniTracks.Models.Trip;
using UniTracks.Services.ApplicationModel.DataTransfer;
using UniTracks.Services.Data;
using UniTracks.Services.IO;
using UniTracks.Services.Location;
using UniTracks.Services.Navigation;
using INavigation = UniTracks.Services.Navigation.INavigation;

namespace UniTracks.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    public Services.Navigation.INavigation Navigation { get; }
    public INavigationRoutes NavigationRoutes { get; }
    public ILocationService LocationService { get; }
    public IShare Share { get; }
    public IFileSystem FileSystem { get; }
    public IGpsDataStorageService GpsDataStorageService { get; }
    public IGenericRepository<SqliteDBContext> SqliteRepository { get; }
    public IGenericLiteDBRepository<ILiteDatabase> LiteDBRepository { get; }
    public string DatabasePath { get; }
    public string LiteDBDatabasePath { get; private set; }

    [ObservableProperty, ]
    private int selectedViewModelIndex = 0;

    [ObservableProperty]
    private ObservableCollection<Location> locations = new ObservableCollection<Location>();

    [ObservableProperty]
    private ObservableCollection<Trip> trips = new ObservableCollection<Trip>();

    [ObservableProperty]
    private string debugText;

    [ObservableProperty]
    private Trip selectedTrip;

    public MainPageViewModel(INavigation navigation, INavigationRoutes navigationRoutes ,ILocationService locationService, IShare share, IFileSystem fileSystem, IGpsDataStorageService gpsDataStorageService, IGenericRepository<SqliteDBContext> sqliteRepository, IGenericLiteDBRepository<ILiteDatabase> liteDBRepository)
    {
        Navigation = navigation;
        NavigationRoutes = navigationRoutes;
        LocationService = locationService;
        Share = share;
        FileSystem = fileSystem;
        GpsDataStorageService = gpsDataStorageService;
        SqliteRepository = sqliteRepository;
        LiteDBRepository = liteDBRepository;
        DatabasePath = Path.Combine(FileSystem.AppDataDirectory, ApplicationConstants.SQliteDatabaseName);
        LiteDBDatabasePath = Path.Combine(FileSystem.AppDataDirectory, ApplicationConstants.LiteDBName);

        StopListening().Await();

    }

    [RelayCommand]
    public async Task StartListening()
    {
        await LocationService.StartListening();
    }

    [RelayCommand]
    public async Task StopListening()
    {
        LocationService.StopListening();

        await LocationfromLastTrip();

    }

    private async Task LocationfromLastTrip()
    {
        Trips.Clear();
        (await SqliteRepository.GetAllAsync<Trip>(trip => trip.Locations)).OrderByDescending(trip => trip.StartTime).ToList().ForEach(async trip =>
        {
            if (trip != null)
            {
                //trip.MaxSpeed = trip.Locations.Max(x => x.Speed);
                //trip.MinSpeed = trip.Locations.Min(x => x.Speed);
                //trip.MaxAltitude = trip.Locations.Max(x => x.Altitude);
                //trip.MinAltitude = trip.Locations.Min(x => x.Altitude);
                //trip.MaxHeading = trip.Locations.Max(x => x.Heading);
                //trip.MinHeading = trip.Locations.Min(x => x.Heading);
                //trip.AverageSpeed = trip.Locations.Average(x => x.Speed);
                //trip.EndTime = trip.Locations.Max(x => x.Timestamp);

                //trip.Distance = calculateDistance(trip.Locations);

                //trip = await SqliteRepository.Update<Trip>(trip);

                Trips.Add(trip);
            }
        });

        if (Trips.Count > 0)
        {
            Locations.Clear();
            Trip lastTrip = Trips.Last();

            Console.WriteLine($"Last Trip: {lastTrip.ID} {lastTrip.StartTime}");
            lastTrip.Locations?.ForEach(location =>
            {
                Locations.Add(location);
            });
        }

        double calculateDistance(List<Location> locations)
        {
            double distance = 0;
            for (int i = 0; i < locations.Count - 1; i++)
            {
                var location1 = locations[i];
                var location2 = locations[i + 1];
                GeoCoordinate coordinate1 = new GeoCoordinate(location1.Latitude, location1.Longitude);
                GeoCoordinate coordinate2 = new GeoCoordinate(location2.Latitude, location2.Longitude);

                distance += coordinate1.GetDistanceTo(coordinate2);
            }

            return distance;
        }
    }

    [RelayCommand]
    public async Task ShareDatabase()
    {
        List<Models.Location.Location> sqliteLocations = (await SqliteRepository.GetAllAsync<Location>()).ToList();
        List<Models.Location.Location> liteDBLocations = (await LiteDBRepository.GetAllAsync<Location>()).ToList();
        Console.WriteLine($"Total SQLite Locations: {sqliteLocations.Count}");
        Console.WriteLine($"Total LiteDB Locations: {liteDBLocations.Count}");

        sqliteLocations.ForEach(async x => Console.WriteLine($"SQLite {x.Timestamp} - {x.ID} - {x.Longitude} - {x.Latitude}"));
        liteDBLocations.ForEach(async x => Console.WriteLine($"LiteDB {x.Timestamp} - {x.ID} - {x.Longitude} - {x.Latitude}"));

        await LocationfromLastTrip();

        await Share.ShareFiles("Share Databases", new string[] { DatabasePath, LiteDBDatabasePath });
    }

    [RelayCommand]
    public async Task ImportDatabase()
    {

    }

    partial void OnSelectedTripChanged(Trip? oldValue, Trip newValue)
    {
        Navigation.NavigateTo(NavigationRoutes.TripOverviewPage, newValue);
    }

    [RelayCommand]
    private void SelectedTripChanged()
    {
        //Navigation.NavigateTo(NavigationRoutes.TripOverviewPage, SelectedTrip);
    }
}

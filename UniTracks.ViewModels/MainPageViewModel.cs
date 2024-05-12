using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoCoordinatePortable;
using System.Collections.ObjectModel;
using System.Linq;
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

namespace UniTracks.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
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

    public MainPageViewModel(ILocationService locationService, IShare share, IFileSystem fileSystem, IGpsDataStorageService gpsDataStorageService, IGenericRepository<SqliteDBContext> sqliteRepository, IGenericLiteDBRepository<ILiteDatabase> liteDBRepository)
    {
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
        (await SqliteRepository.GetAllAsync<Trip>(trip => trip.Locations)).ToList().ForEach(trip =>
        {


            if (trip != null)
            {
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
}

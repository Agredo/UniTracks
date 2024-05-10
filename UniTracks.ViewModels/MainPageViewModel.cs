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

    [ObservableProperty]
    public ObservableCollection<Location> locations = new ObservableCollection<Location>();
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

    //private async Task MigrateOldData()
    //{
    //    List<Location> todaysLocations = SqliteRepository
    //        .Get<Location>(location => location.Timestamp > DateTime.Now.AddHours(-12))
    //        .ToList();
    //    List<Location> yesterdaysLocations = SqliteRepository
    //        .Get<Location>(location => location.Timestamp < DateTime.Now.AddHours(-12))
    //        .ToList();

    //    todaysLocations.OrderBy(location => location.Timestamp);
    //    yesterdaysLocations.OrderBy(location => location.Timestamp);

    //    Trip todaysTrip = null;
    //    Trip yesterdaysTrip = null;

    //    if (todaysLocations.Any())
    //    {
    //        todaysTrip = new Trip()
    //        {
    //            Name = "Bremen Neustadt Run",
    //            StartTime = todaysLocations.First().Timestamp,
    //            EndTime = todaysLocations.Last().Timestamp,
    //            Locations = todaysLocations, 
    //            Distance = CalculateDistance(todaysLocations),
    //        };
    //    }

    //    if (yesterdaysLocations.Any())
    //    {
    //        yesterdaysTrip = new Trip()
    //        {
    //            Name = "Test Run",
    //            StartTime = yesterdaysLocations.First().Timestamp,
    //            EndTime = yesterdaysLocations.Last().Timestamp,
    //            Locations = yesterdaysLocations,
    //            Distance = CalculateDistance(yesterdaysLocations),
    //        };
    //    }

    //    if (yesterdaysTrip is not null && todaysTrip is not null)
    //    {
    //        debugText = $"Name: {yesterdaysTrip.Name} Distance:{yesterdaysTrip.Distance}\nName: {todaysTrip.Name} Distance:{todaysTrip.Distance}";
    //        //await SqliteRepository.Add<Trip>(todaysTrip);
    //        //await SqliteRepository.Add<Trip>(yesterdaysTrip);
    //    }

    //}

    //private double CalculateDistance(List<Location> yesterdaysLocations)
    //{
    //    double distance = 0;
    //    List<GeoCoordinate> locations = yesterdaysLocations.Select(location => new GeoCoordinate(location.Latitude, location.Longitude)).ToList();
    //    for (int i = 0; i < locations.Count - 1; i++) 
    //    {
    //        distance += locations[i].GetDistanceTo(locations[i + 1]);
    //    }

    //    return locations.Zip(locations.Skip(1), (loc1, loc2) => loc1.GetDistanceTo(loc2)).Sum();
    //}

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
        List<Trip> trips = (await SqliteRepository.GetAllAsync<Trip>()).ToList();

        if (trips.Count > 0)
        {
            Locations.Clear();
            Trip lastTrip = trips.Last();

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

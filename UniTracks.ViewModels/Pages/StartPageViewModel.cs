using System.Collections.ObjectModel;
using AgredoApplication.MVVM.Services.Abstractions.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Data.LiteDB;
using UniTracks.Data.Repository;
using UniTracks.Data.SQLite;
using UniTracks.Models.Constants;
using UniTracks.Models.Trip;
using UniTracks.Services.Data;
using UniTracks.Services.Location;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.ViewModels.Pages;

public partial class StartPageViewModel : ObservableObject
{
    public ILocationService LocationService { get; }
    public IFileSystem FileSystem { get; }
    public IGpsDataStorageService GpsDataStorageService { get; }
    public IGenericRepository<SqliteDBContext> SqliteRepository { get; }
    public IGenericLiteDBRepository<ILiteDatabase> LiteDBRepository { get; }
    public string DatabasePath { get; }
    public string LiteDBDatabasePath { get; private set; }

    [ObservableProperty]
    private ObservableCollection<LocationModel> locations = new ObservableCollection<LocationModel>();

    [ObservableProperty]
    private string? debugText;

    public StartPageViewModel(
        ILocationService locationService,
        IFileSystem fileSystem,
        IGpsDataStorageService gpsDataStorageService,
        IGenericRepository<SqliteDBContext> sqliteRepository,
        IGenericLiteDBRepository<ILiteDatabase> liteDBRepository)
    {
        LocationService = locationService;
        FileSystem = fileSystem;
        GpsDataStorageService = gpsDataStorageService;
        SqliteRepository = sqliteRepository;
        LiteDBRepository = liteDBRepository;
        DatabasePath = Path.Combine(FileSystem.AppDataDirectory, ApplicationConstants.SQliteDatabaseName);
        LiteDBDatabasePath = Path.Combine(FileSystem.AppDataDirectory, ApplicationConstants.LiteDBName);

        _ = StopListening();
    }

    [RelayCommand]
    private async Task StartListening()
    {
        await LocationService.StartListening();
    }

    [RelayCommand]
    private async Task StopListening()
    {
        LocationService.StopListening();

        await LoadLocationsFromLastTripAsync();
    }

    private async Task LoadLocationsFromLastTripAsync()
    {
        List<Trip> trips = (await SqliteRepository.GetAllAsync<Trip>(trip => trip.Locations)).ToList();

        if (trips.Count > 0)
        {
            Locations.Clear();
            Trip lastTrip = trips.Last();

            Console.WriteLine($"Last Trip: {lastTrip.ID} {lastTrip.StartTime}");
            lastTrip.Locations?.ForEach(location => Locations.Add(location));
        }
    }

    [RelayCommand]
    private async Task ShareDatabase()
    {
        List<LocationModel> sqliteLocations = (await SqliteRepository.GetAllAsync<LocationModel>()).ToList();
        List<LocationModel> liteDBLocations = (await LiteDBRepository.GetAllAsync<LocationModel>()).ToList();
        Console.WriteLine($"Total SQLite Locations: {sqliteLocations.Count}");
        Console.WriteLine($"Total LiteDB Locations: {liteDBLocations.Count}");

        sqliteLocations.ForEach(x => Console.WriteLine($"SQLite {x.Timestamp} - {x.ID} - {x.Longitude} - {x.Latitude}"));
        liteDBLocations.ForEach(x => Console.WriteLine($"LiteDB {x.Timestamp} - {x.ID} - {x.Longitude} - {x.Latitude}"));

        await LoadLocationsFromLastTripAsync();

        await FileSystem.ShareFilesAsync("Share Databases", new[] { DatabasePath, LiteDBDatabasePath });
    }

    [RelayCommand]
    private async Task ImportDatabase()
    {
        await Task.CompletedTask;
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB;
using UniTracks.Common.ExtensionMethods;
using UniTracks.Data.Repository;
using UniTracks.Data.SQLite;
using UniTracks.Models.Location;
using UniTracks.Services.ApplicationModel;
using UniTracks.Services.ApplicationModel.DataTransfer;
using UniTracks.Services.ApplicationModel.Permissions;
using UniTracks.Services.Location;
using UniTracks.Services.Navigation;
using UniTracks.ViewModels.Core.PermissionUtils;

namespace UniTracks.ViewModels.Pages.Tabs;

public partial class RecordTripTabPageViewModel : ObservableObject
{
    public INavigation Navigation { get; }
    public INavigationRoutes NavigationRoutes { get; }
    public ILocationService LocationService { get; }
    public IShare Share { get; }
    public IPermissions Permissions { get; }
    public IGenericRepository<SqliteDBContext> SqliteRepository { get; }
    public string DatabasePath { get; private set; }

    public RecordTripTabPageViewModel(INavigation navigation, INavigationRoutes navigationRoutes, ILocationService locationService, IShare share, IPermissions permissions, IGenericRepository<SqliteDBContext> sqliteRepository)
    {   
        Navigation = navigation;
        NavigationRoutes = navigationRoutes;
        LocationService = locationService;
        Share = share;
        Permissions = permissions;
        SqliteRepository = sqliteRepository;

        DatabasePath = sqliteRepository.Context.DatabasePath;

        StopListening().Await();
    }

    [RelayCommand]
    public async Task StartListening()
    {
        PermissionStatus locationAlwaysPermissionStatus = await PermissionHelper.CheckAndRequestPermission(Permissions, Permission.LocationAlways);

        if (locationAlwaysPermissionStatus is PermissionStatus.Granted)
        {
            await LocationService.StartListening();
        }
    }

    [RelayCommand]
    public async Task StopListening()
    {
        LocationService.StopListening();

    }

    [RelayCommand]
    public async Task ShareDatabase()
    {
        List<Models.Location.Location> sqliteLocations = (await SqliteRepository.GetAllAsync<Location>()).ToList();
        //List<Models.Location.Location> liteDBLocations = (await LiteDBRepository.GetAllAsync<Location>()).ToList();
        Console.WriteLine($"Total SQLite Locations: {sqliteLocations.Count}");
        //Console.WriteLine($"Total LiteDB Locations: {liteDBLocations.Count}");

        sqliteLocations.ForEach(async x => Console.WriteLine($"SQLite {x.Timestamp} - {x.ID} - {x.Longitude} - {x.Latitude}"));
        //liteDBLocations.ForEach(async x => Console.WriteLine($"LiteDB {x.Timestamp} - {x.ID} - {x.Longitude} - {x.Latitude}"));

        //await LocationfromLastTrip();

        await Share.ShareFiles("Share Databases", new string[] { DatabasePath });
    }
}

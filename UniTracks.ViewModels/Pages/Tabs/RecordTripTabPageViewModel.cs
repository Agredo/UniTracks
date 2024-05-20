using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB;
using System.Runtime.CompilerServices;
using UniTracks.Common.Contants;
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
    public IMainThread MainThread { get; }
    public string DatabasePath { get; private set; }

    private string redColor = "#FF0000";
    private string greenColor = "#00FF00";
    private string blueColor = "#0000FF";
    private string whiteColor = "#FFFFFF";

    private bool isRecording = false;

    public RecordTripTabPageViewModel(INavigation navigation, INavigationRoutes navigationRoutes, ILocationService locationService, IShare share, IPermissions permissions, IGenericRepository<SqliteDBContext> sqliteRepository, IMainThread mainThread)
    {   
        Navigation = navigation;
        NavigationRoutes = navigationRoutes;
        LocationService = locationService;
        Share = share;
        Permissions = permissions;
        SqliteRepository = sqliteRepository;
        MainThread = mainThread;
        DatabasePath = sqliteRepository.Context.DatabasePath;
        RecordIconSourceString = $"{ApplicationConstants.RawIconBasePath}{ApplicationIconConstants.PlayIcon}";

        RecordIconColor = whiteColor;

        StopListening().Await();
    }

    [ObservableProperty]
    private string recordIconSourceString;

    [ObservableProperty]
    private string recordIconColor;

    [RelayCommand]
    public async Task StartListening()
    {
        if (isRecording)
        {
            RecordIconColor = whiteColor;
            RecordIconSourceString = $"{ApplicationConstants.RawIconBasePath}{ApplicationIconConstants.PlayIcon}";
            isRecording = false;

            LocationService.StopListening();
        }
        else
        {
            isRecording = true;
            RecordIconColor = redColor;
            RecordIconSourceString = $"{ApplicationConstants.RawIconBasePath}{ApplicationIconConstants.StopIcon}";

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                PermissionStatus locationAlwaysPermissionStatus = await PermissionHelper.CheckAndRequestPermission(Permissions, Permission.LocationAlways);

                if (locationAlwaysPermissionStatus is PermissionStatus.Granted)
                {
                    await LocationService.StartListening();
                }
            });
        }
    }

    [RelayCommand]
    public async Task StopListening()
    {
        LocationService.StopListening();
        RecordIconColor = whiteColor;
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

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using UniTracks.Common.Contants;
using UniTracks.Data.Repository;
using UniTracks.Data.SQLite;
using UniTracks.Models.User;
using UniTracks.Services.ApplicationModel;
using UniTracks.Services.ApplicationModel.Permissions;
using UniTracks.Services.Dispatching;
using UniTracks.Services.Location;
using UniTracks.Services.Navigation;
using UniTracks.ViewModels.Controls.Popups;
using UniTracks.ViewModels.Core.PermissionUtils;

namespace UniTracks.ViewModels.Pages.Tabs;

public partial class RecordTripTabPageViewModel : ObservableObject
{
    public INavigation Navigation { get; }
    public IPopupNavigation PopupNavigation { get; }
    public INavigationRoutes NavigationRoutes { get; }
    public ILocationService LocationService { get; }
    public IPermissions Permissions { get; }
    public IMainThread MainThread { get; }
    public IDispatcher Dispatcher { get; }
    public IGenericRepository<SqliteDBContext> SqliteRepository { get; }
    public string DatabasePath { get; private set; }

    private string redColor = "#FF0000";
    private string greenColor = "#00FF00";
    private string blueColor = "#0000FF";
    private string whiteColor = "#FFFFFF";

    private bool isRecording = false;

    Stopwatch stopWatch = new Stopwatch();

    EventHandler StopWatchEventHandler;

    public RecordTripTabPageViewModel(INavigation navigation, 
        IPopupNavigation popupNavigation, 
        INavigationRoutes navigationRoutes, 
        ILocationService locationService, 
        IPermissions permissions, 
        IMainThread mainThread, 
        IDispatcher dispatcher,
        IGenericRepository<SqliteDBContext> sqliteRepository)
    {   
        Navigation = navigation;
        PopupNavigation = popupNavigation;
        NavigationRoutes = navigationRoutes;
        LocationService = locationService;
        Permissions = permissions;
        MainThread = mainThread;
        Dispatcher = dispatcher;
        SqliteRepository = sqliteRepository;
        RecordIconSourceString = $"{ApplicationConstants.RawIconBasePath}{ApplicationIconConstants.PlayIcon}";
        RecordIconColor = whiteColor;

        StopWatchEventHandler = (sender, e) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StopWatchTime = stopWatch.Elapsed.ToString(@"hh\:mm\:ss\.fff");
            });

        };

        Dispatcher.CreateTimer(TimeSpan.FromMilliseconds(100));
        Dispatcher.AddEventHandler(StopWatchEventHandler);

        StopListening();
    }

    [ObservableProperty]
    private string recordIconSourceString;

    [ObservableProperty]
    private string recordIconColor;

    [ObservableProperty]
    private string stopWatchTime = "00:00:000";

    [RelayCommand]
    public async Task StartListening()
    {

        if (!(await SqliteRepository.GetAllAsync<User>()).Any())
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await PopupNavigation.ShowPopupAsync<UserCreationPopupViewModel>();
            });
        }

        if (isRecording)
        {
            RecordIconColor = whiteColor;
            RecordIconSourceString = $"{ApplicationConstants.RawIconBasePath}{ApplicationIconConstants.PlayIcon}";
            isRecording = false;

            LocationService.StopListening();
            
            Dispatcher.StopTimer();
            stopWatch.Stop();
        }
        else
        {
            isRecording = true;
            RecordIconColor = redColor;
            RecordIconSourceString = $"{ApplicationConstants.RawIconBasePath}{ApplicationIconConstants.StopIcon}";

            stopWatch.Restart();
            Dispatcher.StartTimer();

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
    public void StopListening()
    {
        LocationService.StopListening();
        Dispatcher.StopTimer();
        stopWatch.Stop();
        

        RecordIconColor = whiteColor;
    }
}

using System.Diagnostics;
using AgredoApplication.MVVM.Services.Abstractions.Application;
using AgredoApplication.MVVM.Services.Abstractions.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Data.Repository;
using UniTracks.Data.SQLite;
using UniTracks.Models.Constants;
using UniTracks.Models.User;
using UniTracks.Services.ApplicationModel;
using UniTracks.Services.ApplicationModel.Permissions;
using UniTracks.Services.Dispatching;
using UniTracks.Services.Location;
using UniTracks.ViewModels.Controls.Popups;
using UniTracks.ViewModels.PermissionUtils;

namespace UniTracks.ViewModels.Pages.Tabs;

public partial class RecordTripTabPageViewModel : ObservableObject
{
    public INavigationService Navigation { get; }
    public IPopupNavigationService PopupNavigation { get; }
    public ILocationService LocationService { get; }
    public IPermissions Permissions { get; }
    public IMainThread MainThread { get; }
    public IDispatcher Dispatcher { get; }
    public IGenericRepository<SqliteDBContext> SqliteRepository { get; }
    public string DatabasePath { get; private set; }

    private const string RedColor = "#FF0000";
    private const string WhiteColor = "#FFFFFF";

    private readonly Stopwatch stopWatch = new Stopwatch();

    private readonly EventHandler stopWatchEventHandler;

    public RecordTripTabPageViewModel(
        INavigationService navigation,
        IPopupNavigationService popupNavigation,
        ILocationService locationService,
        IPermissions permissions,
        IMainThread mainThread,
        IDispatcher dispatcher,
        IGenericRepository<SqliteDBContext> sqliteRepository)
    {
        Navigation = navigation;
        PopupNavigation = popupNavigation;
        LocationService = locationService;
        Permissions = permissions;
        MainThread = mainThread;
        Dispatcher = dispatcher;
        SqliteRepository = sqliteRepository;
        DatabasePath = string.Empty;
        RecordIconSourceString = $"{ApplicationConstants.RawIconBasePath}{ApplicationIconConstants.PlayIcon}";
        RecordIconColor = WhiteColor;

        stopWatchEventHandler = (sender, e) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StopWatchTime = stopWatch.Elapsed.ToString(@"hh\:mm\:ss\.fff");
            });
        };

        Dispatcher.CreateTimer(TimeSpan.FromMilliseconds(100));
        Dispatcher.AddEventHandler(stopWatchEventHandler);

        StopListening();
    }

    [ObservableProperty]
    private string recordIconSourceString = string.Empty;

    [ObservableProperty]
    private string recordIconColor = string.Empty;

    [ObservableProperty]
    private string stopWatchTime = "00:00:000";

    [ObservableProperty]
    private bool isRecording;

    [ObservableProperty]
    private string statusText = "Bereit";

    [RelayCommand]
    private async Task StartListening()
    {
        if (!(await SqliteRepository.GetAllAsync<User>()).Any())
        {
            await PopupNavigation.ShowPopupAsync<UserCreationPopupViewModel>();
        }

        if (IsRecording)
        {
            RecordIconColor = WhiteColor;
            RecordIconSourceString = $"{ApplicationConstants.RawIconBasePath}{ApplicationIconConstants.PlayIcon}";
            IsRecording = false;
            StatusText = "Pausiert";

            LocationService.StopListening();

            Dispatcher.StopTimer();
            stopWatch.Stop();
        }
        else
        {
            IsRecording = true;
            StatusText = "Aufnahme läuft";
            RecordIconColor = RedColor;
            RecordIconSourceString = $"{ApplicationConstants.RawIconBasePath}{ApplicationIconConstants.StopIcon}";

            stopWatch.Restart();
            Dispatcher.StartTimer();

            PermissionStatus locationAlwaysPermissionStatus = await PermissionHelper.CheckAndRequestPermission(Permissions, Permission.LocationAlways);

            if (locationAlwaysPermissionStatus is PermissionStatus.Granted)
            {
                await LocationService.StartListening();
            }
        }
    }

    [RelayCommand]
    private void StopListening()
    {
        LocationService.StopListening();
        Dispatcher.StopTimer();
        stopWatch.Stop();

        IsRecording = false;
        StatusText = "Bereit";
        RecordIconColor = WhiteColor;
    }
}

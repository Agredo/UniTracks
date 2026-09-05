using System.Collections.ObjectModel;
using System.Diagnostics;
using AgredoApplication.MVVM.Services.Abstractions.Application;
using AgredoApplication.MVVM.Services.Abstractions.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Data.Repository;
using UniTracks.Models.Constants;
using UniTracks.Models.Trip;
using UniTracks.Models.User;
using UniTracks.Services.ApplicationModel;
using UniTracks.Services.ApplicationModel.Permissions;
using UniTracks.Services.Data;
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
    public IRepository Repository { get; }
    public IGpsDataStorageService GpsDataStorageService { get; }
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
        IRepository repository,
        IGpsDataStorageService gpsDataStorageService)
    {
        Navigation = navigation;
        PopupNavigation = popupNavigation;
        LocationService = locationService;
        Permissions = permissions;
        MainThread = mainThread;
        Dispatcher = dispatcher;
        Repository = repository;
        GpsDataStorageService = gpsDataStorageService;
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
        _ = LoadTripTypesAsync();
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

    [ObservableProperty]
    private TripType? selectedTripType;

    public ObservableCollection<TripType> TripTypes { get; } = new();

    /// <summary>The activity chip selector is only editable while not recording.</summary>
    public bool IsTripTypeSelectionVisible => !IsRecording;

    partial void OnIsRecordingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsTripTypeSelectionVisible));
    }

    partial void OnSelectedTripTypeChanged(TripType? value)
    {
        GpsDataStorageService.CurrentTripTypeId = value?.ID;

        // Move the freshly picked type to the front so frequently used types stay on top.
        if (value is not null && TripTypes.IndexOf(value) > 0)
        {
            TripTypes.Move(TripTypes.IndexOf(value), 0);
        }
    }

    private async Task LoadTripTypesAsync()
    {
        var types = (await Repository.GetAllAsync<TripType>()).ToList();

        // Order by usage: most recently used first, then highest usage count, then seed order.
        var usage = Repository.Get<Trip>(t => t.TripTypeId != null)
            .GroupBy(t => t.TripTypeId!.Value)
            .ToDictionary(
                g => g.Key,
                g => new { Count = g.Count(), Last = g.Max(t => t.StartTime) });

        var ordered = types
            .OrderByDescending(t => usage.TryGetValue(t.ID, out var u) ? u.Last : DateTimeOffset.MinValue)
            .ThenByDescending(t => usage.TryGetValue(t.ID, out var u) ? u.Count : 0)
            .ToList();

        TripTypes.Clear();
        foreach (var type in ordered)
        {
            TripTypes.Add(type);
        }

        SelectedTripType ??= TripTypes.FirstOrDefault();
    }

    [RelayCommand]
    private async Task StartListening()
    {
        if (!(await Repository.GetAllAsync<User>()).Any())
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

            GpsDataStorageService.CurrentTripTypeId = SelectedTripType?.ID;

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
        GpsDataStorageService.FinalizeTrip();
        Dispatcher.StopTimer();
        stopWatch.Stop();

        IsRecording = false;
        StatusText = "Bereit";
        RecordIconColor = WhiteColor;
    }
}

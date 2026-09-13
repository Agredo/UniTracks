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

    /// <summary>How often the watchdog checks whether the platform still delivers locations.</summary>
    private const int WatchdogIntervalSeconds = 5;

    /// <summary>
    /// A moving recording delivers about one fix per second, so a longer silence means the platform
    /// stopped. iOS can end background updates without any error; the UI must not keep claiming that
    /// a recording is running while nothing is captured.
    /// </summary>
    private static readonly TimeSpan FixSilenceTolerance = TimeSpan.FromSeconds(45);

    private CancellationTokenSource? healthWatchdog;
    private bool healthWarningActive;

    private void StartHealthWatchdog()
    {
        StopHealthWatchdog();

        healthWarningActive = false;

        var cancellation = new CancellationTokenSource();
        healthWatchdog = cancellation;

        _ = RunHealthWatchdogAsync(cancellation.Token);
    }

    private void StopHealthWatchdog()
    {
        CancellationTokenSource? cancellation = Interlocked.Exchange(ref healthWatchdog, null);

        if (cancellation is null)
        {
            return;
        }

        try
        {
            cancellation.Cancel();
            cancellation.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // Already cancelled by a previous stop.
        }
    }

    private async Task RunHealthWatchdogAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(WatchdogIntervalSeconds));

        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                if (!IsRecording)
                {
                    continue;
                }

                string? warning = DescribeCaptureProblem(LocationService.Health);

                if (warning is null)
                {
                    healthWarningActive = false;
                    continue;
                }

                if (healthWarningActive)
                {
                    continue;
                }

                healthWarningActive = true;
                LocationDiagnostics.Write($"WARNUNG Aufzeichnung: {warning}");
                MainThread.BeginInvokeOnMainThread(() => StatusText = warning);
            }
        }
        catch (OperationCanceledException)
        {
            // Recording stopped; the watchdog is no longer needed.
        }
    }

    /// <summary>
    /// Maps a capture snapshot to a user-facing warning, or null when the capture looks healthy.
    /// </summary>
    public static string? DescribeCaptureProblem(LocationCaptureHealth health)
    {
        // Platforms that do not report capture details (Android foreground service, desktop) must
        // not be turned into a warning: their "unknown" state is not a stopped capture.
        if (!health.IsTracked)
        {
            return null;
        }

        if (!health.IsRunning)
        {
            return "Aufzeichnung angehalten – Standortpunkte fehlen";
        }

        if (health.LastFixAt is { } lastFix)
        {
            TimeSpan silence = DateTimeOffset.Now - lastFix;

            if (silence > FixSilenceTolerance)
            {
                return $"Seit {silence.TotalSeconds:F0} s kein Standortpunkt";
            }
        }

        return null;
    }

    partial void OnIsRecordingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsTripTypeSelectionVisible));
    }

    [RelayCommand]
    private async Task SearchTripType()
    {
        var selected = await PopupNavigation.ShowPopupAsync<TripTypeSearchPopupViewModel, TripType?>();
        if (selected is not null)
        {
            SelectedTripType = selected;
        }
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
        // Read asynchronously: a filtered read still scans the whole trip collection (with the GPS
        // points embedded in it), which used to block the UI thread every time this tab was opened.
        var usage = (await Repository.GetAsync<Trip>(t => t.TripTypeId != null))
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

            StopHealthWatchdog();
            LocationService.StopListening();

            Dispatcher.StopTimer();
            stopWatch.Stop();
            return;
        }

        // Ask for the permission before the UI claims that a recording is running. The status was
        // switched first and the result only used to decide whether to start listening, so a denied
        // permission left the page showing "Aufnahme läuft" with a running timer while nothing was
        // recorded.
        //
        // Android wird zweistufig gefragt: erst "Beim Verwenden" (das ist die Freigabe, die der
        // Dialog anbietet und die der im Vordergrund gestartete location-Foreground-Service fuer die
        // Hintergrundaufzeichnung braucht), dann "Immer". ACCESS_BACKGROUND_LOCATION steht inzwischen
        // im Manifest, sonst wuerde Permissions.LocationAlways eine PermissionException werfen (das
        // hat die App hier beim Start der Aufnahme abgestuerzt). Ab Android 11 bietet der Dialog
        // "Immer erlauben" nicht mehr an: Die Anfrage kommt ohne Dialog als "abgelehnt" zurueck, die
        // Aufnahme laeuft trotzdem, und die Einstellungen-Seite fuehrt auf die Systemseite.
        bool isAndroid = OperatingSystem.IsAndroid();
        Permission locationPermission = PermissionHelper.PrimaryLocationPermission(isAndroid);

        PermissionStatus locationPermissionStatus = await PermissionHelper.CheckAndRequestPermission(Permissions, locationPermission);

        if (locationPermissionStatus is not PermissionStatus.Granted)
        {
            StatusText = "Standortfreigabe fehlt";
            return;
        }

        bool backgroundLocationMissing = false;

        if (PermissionHelper.RequiresBackgroundLocationStep(isAndroid, locationPermissionStatus))
        {
            PermissionStatus backgroundStatus = await PermissionHelper.CheckAndRequestPermission(Permissions, Permission.LocationAlways);
            backgroundLocationMissing = backgroundStatus is not PermissionStatus.Granted;

            LocationDiagnostics.Write(
                $"Android Hintergrund-Freigabe (ACCESS_BACKGROUND_LOCATION): {backgroundStatus}.");
        }

        GpsDataStorageService.CurrentTripTypeId = SelectedTripType?.ID;

        IsRecording = true;
        StatusText = backgroundLocationMissing
            ? "Aufnahme läuft – Immer-Freigabe offen"
            : "Aufnahme läuft";
        RecordIconColor = RedColor;
        RecordIconSourceString = $"{ApplicationConstants.RawIconBasePath}{ApplicationIconConstants.StopIcon}";

        // Start() continues a paused recording. Restart() reset the elapsed time to 00:00 on every
        // resume, so pausing and continuing threw the already recorded duration away.
        stopWatch.Start();
        Dispatcher.StartTimer();
        StartHealthWatchdog();

        await LocationService.StartListening();
    }

    [RelayCommand]
    private async Task StopListening()
    {
        // Stop capture and wait for the drain window first: iOS may still hold locations of the
        // trip, and finalising before they arrive drops them from the finished trip.
        await LocationService.StopListeningAndDrainAsync();

        // Only close a trip that is actually running. Finalising unconditionally used to end a
        // recording whenever this command ran while a trip was in progress.
        if (GpsDataStorageService.IsTripInProgress)
        {
            GpsDataStorageService.FinalizeTrip();
        }

        StopHealthWatchdog();
        Dispatcher.StopTimer();
        stopWatch.Stop();

        // A full stop ends the recording, so the next one has to begin at 00:00 again; Stop()
        // alone keeps the elapsed time and would let a new recording continue the previous clock.
        stopWatch.Reset();
        StopWatchTime = "00:00:000";

        IsRecording = false;
        StatusText = "Bereit";
        RecordIconColor = WhiteColor;
    }
}

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
using UniTracks.ViewModels.Recording;

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

    /// <summary>
    /// Lock-screen controls of the running recording. Tapping one of them here is the same as tapping
    /// the button on the page, so the runner can pause and stop without unlocking the phone.
    /// </summary>
    public IRecordingRemoteControls RemoteControls { get; }

    public string DatabasePath { get; private set; }

    /// <summary>
    /// What the next recording will be tagged with. Rendered as the context bar on the page; new
    /// recording attributes (shoes, heart rate, ...) are added here and show up alongside the type.
    /// </summary>
    public RecordingContext Context { get; } = new();

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
        IGpsDataStorageService gpsDataStorageService,
        IRecordingRemoteControls remoteControls)
    {
        Navigation = navigation;
        PopupNavigation = popupNavigation;
        LocationService = locationService;
        Permissions = permissions;
        MainThread = mainThread;
        Dispatcher = dispatcher;
        Repository = repository;
        GpsDataStorageService = gpsDataStorageService;
        RemoteControls = remoteControls;
        DatabasePath = string.Empty;
        RecordIconSourceString = $"{ApplicationConstants.RawIconBasePath}{ApplicationIconConstants.PlayIcon}";
        RecordIconColor = WhiteColor;

        stopWatchEventHandler = (sender, e) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StopWatchTime = FormatElapsed(stopWatch.Elapsed);
            });
        };

        // The clock shows whole seconds only, so a quarter-second tick keeps it fresh without ten
        // main-thread hops per second.
        Dispatcher.CreateTimer(TimeSpan.FromMilliseconds(250));
        Dispatcher.AddEventHandler(stopWatchEventHandler);

        // The lock screen raises its commands from its own thread and while the page is off screen, so
        // every command is marshalled to the main thread and applied against the current state.
        RemoteControls.PauseRequested += (_, _) => RunRemoteCommand(RemoteCommand.Pause);
        RemoteControls.ResumeRequested += (_, _) => RunRemoteCommand(RemoteCommand.Resume);
        RemoteControls.StopRequested += (_, _) => RunRemoteCommand(RemoteCommand.Stop);

        _ = LoadTripTypesAsync();
    }

    [ObservableProperty]
    private string recordIconSourceString = string.Empty;

    [ObservableProperty]
    private string recordIconColor = string.Empty;

    [ObservableProperty]
    private string stopWatchTime = "00:00";

    [ObservableProperty]
    private bool isRecording;

    /// <summary>
    /// True while a recording is suspended but the trip is still open. Separate from
    /// <see cref="IsRecording"/> so a pause can no longer be mistaken for an end of the recording -
    /// neither on the page nor on the lock screen.
    /// </summary>
    [ObservableProperty]
    private bool isPaused;

    [ObservableProperty]
    private string statusText = "Bereit";

    [ObservableProperty]
    private TripType? selectedTripType;

    /// <summary>Shown large in the context bar, so the active type cannot be overlooked.</summary>
    public string SelectedTripTypeName => SelectedTripType?.Name ?? "Aktivität wählen";

    public ObservableCollection<TripType> TripTypes { get; } = new();

    /// <summary>
    /// The types offered as one-tap chips: the most relevant ones by usage, with a freshly picked
    /// type floating to the front. The full list stays reachable through the search popup.
    /// </summary>
    public ObservableCollection<TripType> FavoriteTripTypes { get; } = new();

    /// <summary>How many quick-pick chips fit the context bar.</summary>
    private const int MaxFavoriteTripTypes = 4;

    /// <summary>The activity chip selector is only editable while not recording.</summary>
    public bool IsTripTypeSelectionVisible => !IsRecording;

    /// <summary>Stop is only meaningful while a trip is open (recording or paused).</summary>
    public bool CanStop => IsRecording || IsPaused;

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
        OnPropertyChanged(nameof(CanStop));
    }

    partial void OnIsPausedChanged(bool value)
    {
        OnPropertyChanged(nameof(CanStop));
    }

    [RelayCommand]
    private void SelectTripType(TripType tripType)
    {
        SelectedTripType = tripType;
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
        Context.TripType = value;

        // The type is not only a setting for the next trip: while a recording is running the trip row
        // already exists (it is written with the first GPS fix), so the change has to be written to it.
        // Otherwise the finished trip kept the type it had been created with and only a manual edit in
        // the trip overview took the new one over.
        _ = ApplyTripTypeAsync(value);
        OnPropertyChanged(nameof(SelectedTripTypeName));

        if (value is null)
        {
            return;
        }

        // The full list keeps its stable usage order; only the quick-pick chips float the freshly
        // picked type to the front so frequent types stay one tap away.
        int favoriteIndex = FavoriteTripTypes.IndexOf(value);

        if (favoriteIndex > 0)
        {
            FavoriteTripTypes.Move(favoriteIndex, 0);
        }
        else if (favoriteIndex < 0)
        {
            FavoriteTripTypes.Insert(0, value);

            while (FavoriteTripTypes.Count > MaxFavoriteTripTypes)
            {
                FavoriteTripTypes.RemoveAt(FavoriteTripTypes.Count - 1);
            }
        }
    }

    /// <summary>
    /// Stores the selected type. Called from a property change, so it cannot be awaited: the page has
    /// already switched over, and a failed write must not take the app down with it.
    /// </summary>
    private async Task ApplyTripTypeAsync(TripType? tripType)
    {
        try
        {
            await GpsDataStorageService.ApplyTripTypeAsync(tripType?.ID);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"[UniTracks] Sportart {tripType?.Name} konnte nicht gespeichert werden: {exception}");
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
        FavoriteTripTypes.Clear();
        foreach (var type in ordered)
        {
            TripTypes.Add(type);

            if (FavoriteTripTypes.Count < MaxFavoriteTripTypes)
            {
                FavoriteTripTypes.Add(type);
            }
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
            PauseRecording();
            return;
        }

        await StartOrResumeAsync();
    }

    /// <summary>
    /// Pauses the recording: the capture is suspended, but the trip stays open and the platform session
    /// is kept alive so continuing can reuse it. The clock keeps its elapsed time - a pause is not the
    /// end of the recording.
    /// </summary>
    private void PauseRecording()
    {
        RecordIconColor = WhiteColor;
        RecordIconSourceString = $"{ApplicationConstants.RawIconBasePath}{ApplicationIconConstants.PlayIcon}";
        IsRecording = false;
        IsPaused = true;
        StatusText = "Pausiert";

        StopHealthWatchdog();
        LocationService.PauseListening();

        Dispatcher.StopTimer();
        stopWatch.Stop();

        RemoteControls.Update(RecordingRemoteState.Paused, stopWatch.Elapsed);
    }

    /// <summary>
    /// Starts a new recording or continues a paused one. Both are the same operation for the platform:
    /// it is asked to listen again, and the clock keeps counting from where it was.
    /// </summary>
    private async Task StartOrResumeAsync()
    {
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

        bool notificationsMissing = false;

        if (isAndroid)
        {
            // Android 13+ hides the whole foreground notification without this permission - and with it
            // the lock-screen buttons for pause and stop, which is why it is asked for here.
            PermissionStatus notificationStatus =
                await PermissionHelper.CheckAndRequestPermission(Permissions, Permission.PostNotifications);
            notificationsMissing = notificationStatus is not PermissionStatus.Granted;

            LocationDiagnostics.Write($"Android Mitteilungs-Freigabe (POST_NOTIFICATIONS): {notificationStatus}.");
        }

        // The type of a paused trip can have been changed since it was opened, so it is written to the
        // open trip here as well instead of only being remembered for the next one.
        await GpsDataStorageService.ApplyTripTypeAsync(SelectedTripType?.ID);

        IsPaused = false;
        IsRecording = true;
        StatusText = DescribeRecordingStatus(backgroundLocationMissing, notificationsMissing);
        RecordIconColor = RedColor;
        RecordIconSourceString = $"{ApplicationConstants.RawIconBasePath}{ApplicationIconConstants.StopIcon}";

        // Start() continues a paused recording. Restart() reset the elapsed time to 00:00 on every
        // resume, so pausing and continuing threw the already recorded duration away.
        stopWatch.Start();
        Dispatcher.StartTimer();
        StartHealthWatchdog();

        await LocationService.StartListening();

        // Tell the lock screen what is running and how long, so its buttons match the app.
        RemoteControls.Update(RecordingRemoteState.Recording, stopWatch.Elapsed);
    }

    /// <summary>Stopwatch label: compact "mm:ss", expanding to "h:mm:ss" past one hour.</summary>
    public static string FormatElapsed(TimeSpan elapsed) =>
        elapsed.TotalHours >= 1
            ? elapsed.ToString(@"h\:mm\:ss")
            : elapsed.ToString(@"mm\:ss");

    private static string DescribeRecordingStatus(bool backgroundLocationMissing, bool notificationsMissing)
    {
        List<string> missing = new();

        if (backgroundLocationMissing)
        {
            missing.Add("Immer-Freigabe offen");
        }

        if (notificationsMissing)
        {
            missing.Add("Mitteilungen aus");
        }

        return missing.Count == 0
            ? "Aufnahme läuft"
            : $"Aufnahme läuft – {string.Join(", ", missing)}";
    }

    [RelayCommand]
    private async Task StopListening()
    {
        // Stop capture and wait for the drain window first: iOS may still hold locations of the
        // trip, and finalising before they arrive drops them from the finished trip.
        await LocationService.StopListeningAndDrainAsync();

        // No recording is left, so the lock screen must not keep offering its buttons.
        RemoteControls.Update(RecordingRemoteState.Stopped, TimeSpan.Zero);

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
        StopWatchTime = "00:00";

        IsPaused = false;
        IsRecording = false;
        StatusText = "Bereit";
        RecordIconColor = WhiteColor;
    }

    /// <summary>Command a lock-screen button sent (see <see cref="IRecordingRemoteControls"/>).</summary>
    private enum RemoteCommand
    {
        Pause,
        Resume,
        Stop,
    }

    private void RunRemoteCommand(RemoteCommand command)
    {
        MainThread.BeginInvokeOnMainThread(() => _ = ApplyRemoteCommandAsync(command));
    }

    /// <summary>
    /// Runs a command that came from the lock screen. A command that does not fit the current state (a
    /// second tap, a button of a surface that is already gone) is ignored instead of stopping or
    /// starting the wrong thing.
    /// </summary>
    private async Task ApplyRemoteCommandAsync(RemoteCommand command)
    {
        try
        {
            switch (command)
            {
                case RemoteCommand.Pause when IsRecording:
                    PauseRecording();
                    break;

                case RemoteCommand.Resume when IsPaused:
                    await StartOrResumeAsync();
                    break;

                case RemoteCommand.Stop when IsRecording || IsPaused:
                    await StopListening();
                    break;
            }
        }
        catch (Exception ex)
        {
            // A remote command has no caller to report to, so it must never tear the app down.
            Debug.WriteLine($"[UniTracks] Sperrbildschirm-Befehl {command} fehlgeschlagen: {ex}");
        }
    }
}

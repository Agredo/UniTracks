using System.ComponentModel;
using UniTracks.Models.Constants;
using UniTracks.Models.Trip;
using UniTracks.Models.User;
using UniTracks.Services.ApplicationModel;
using UniTracks.Services.ApplicationModel.Permissions;
using UniTracks.Services.Location;
using UniTracks.Tests.ViewModels.Fakes;
using UniTracks.ViewModels.Controls.Popups;
using UniTracks.ViewModels.Pages.Tabs;

namespace UniTracks.Tests.ViewModels;

/// <summary>
/// Tests for <see cref="RecordTripTabPageViewModel"/>. This ViewModel carries the two regressions
/// from the app review — the permission handling (#3) and the pause/resume clock (#4) — and had no
/// coverage at all before this suite.
/// </summary>
public sealed class RecordTripTabPageViewModelTests
{
    private const string PlayIcon = ApplicationConstants.RawIconBasePath + ApplicationIconConstants.PlayIcon;
    private const string StopIcon = ApplicationConstants.RawIconBasePath + ApplicationIconConstants.StopIcon;

    /// <summary>
    /// The constructor registers a timer and calls a fire-and-forget <c>_ = LoadTripTypesAsync()</c>,
    /// so every fake has to be wired up before it is built. It deliberately does <em>not</em> stop
    /// listening any more: creating the page used to end a running recording.
    /// </summary>
    private sealed class Fixture
    {
        public Fixture(Action<InMemoryRepository>? seed = null, bool seedUser = true)
        {
            if (seedUser)
            {
                Repository.Seed(new User { ID = Guid.NewGuid(), Name = "Testfahrer" });
            }

            seed?.Invoke(Repository);

            ViewModel = new RecordTripTabPageViewModel(
                Navigation,
                Popups,
                Location,
                Permissions,
                MainThread,
                Dispatcher,
                Repository,
                Gps,
                Remote);
        }

        public FakeNavigationService Navigation { get; } = new();

        public FakePopupNavigationService Popups { get; } = new();

        public FakeLocationService Location { get; } = new();

        public FakePermissions Permissions { get; } = new();

        public FakeMainThread MainThread { get; } = new();

        public FakeDispatcher Dispatcher { get; } = new();

        public InMemoryRepository Repository { get; } = new();

        public FakeGpsDataStorageService Gps { get; } = new();

        public FakeRecordingRemoteControls Remote { get; } = new();

        public RecordTripTabPageViewModel ViewModel { get; }
    }

    private static TripType NewTripType(string name) => new()
    {
        ID = Guid.NewGuid(),
        Name = name,
        Identifier = name.ToLowerInvariant(),
        Description = name,
        Category = "Test",
    };

    private static Trip NewTrip(DateTimeOffset startTime, Guid? tripTypeId = null) => new()
    {
        ID = Guid.NewGuid(),
        Name = $"Trip {startTime:yyyy-MM-dd}",
        StartTime = startTime,
        EndTime = startTime.AddHours(1),
        TripTypeId = tripTypeId,
        Locations = new(),
    };

    /// <summary>Parses the clock label's adaptive format: "mm:ss" below one hour, "h:mm:ss" above.</summary>
    private static TimeSpan ParseElapsed(string text)
    {
        var parts = text.Split(':');
        return parts.Length == 3
            ? new TimeSpan(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]))
            : new TimeSpan(0, int.Parse(parts[0]), int.Parse(parts[1]));
    }

    [Fact]
    public void Constructor_StartsIdleAndConfiguresTheClock()
    {
        var fixture = new Fixture();

        Assert.False(fixture.ViewModel.IsRecording);
        Assert.Equal("Bereit", fixture.ViewModel.StatusText);
        Assert.Equal("00:00", fixture.ViewModel.StopWatchTime);
        Assert.Equal(PlayIcon, fixture.ViewModel.RecordIconSourceString);
        Assert.Equal("#FFFFFF", fixture.ViewModel.RecordIconColor);

        Assert.Equal(1, fixture.Dispatcher.CreateTimerCalls);
        Assert.Equal(TimeSpan.FromMilliseconds(250), fixture.Dispatcher.TimerInterval!.Value);

        // Creating the page must not touch an in-flight recording. The constructor used to stop
        // listening and finalise the trip, which silently ended a recording whenever this page was
        // re-created (every navigation, and on app start).
        Assert.Equal(0, fixture.Location.StopListeningCalls);
        Assert.Equal(0, fixture.Location.StopListeningAndDrainCalls);
        Assert.Equal(0, fixture.Gps.FinalizeTripCalls);
    }

    /// <summary>
    /// Finding #3: the permission was requested <em>after</em> <c>IsRecording</c> had already been set,
    /// so a denied permission left the page claiming "Aufnahme läuft" with a running timer while
    /// nothing was being recorded. The state must now stay idle and nothing may start.
    /// </summary>
    [Fact]
    public async Task StartListening_WithDeniedPermission_DoesNotStartRecording()
    {
        var fixture = new Fixture();
        fixture.Permissions.Status = PermissionStatus.Denied;

        await fixture.ViewModel.StartListeningCommand.ExecuteAsync(null);

        Assert.False(fixture.ViewModel.IsRecording);
        Assert.Equal("Standortfreigabe fehlt", fixture.ViewModel.StatusText);
        Assert.Equal(PlayIcon, fixture.ViewModel.RecordIconSourceString);
        Assert.Equal("#FFFFFF", fixture.ViewModel.RecordIconColor);

        // Neither the GPS listener nor the clock was started.
        Assert.Equal(0, fixture.Location.StartListeningCalls);
        Assert.Equal(0, fixture.Dispatcher.StartTimerCalls);

        // The permission is checked and requested before any UI state changes.
        Assert.Equal(new[] { Permission.LocationAlways }, fixture.Permissions.CheckedPermissions);
        Assert.Equal(new[] { Permission.LocationAlways }, fixture.Permissions.RequestedPermissions);

        // OperatingSystem.IsAndroid() is constant false on the net11.0 test TFM, so the Android-only
        // LocationWhenInUse fallback is unreachable here and deliberately not covered.
        Assert.DoesNotContain(Permission.LocationWhenInUse, fixture.Permissions.CheckedPermissions);
    }

    [Fact]
    public async Task StartListening_WithGrantedPermission_StartsRecording()
    {
        var tripType = NewTripType("Wandern");
        var fixture = new Fixture(repository => repository.Seed(tripType));
        fixture.Permissions.Status = PermissionStatus.Granted;

        await fixture.ViewModel.StartListeningCommand.ExecuteAsync(null);

        Assert.True(fixture.ViewModel.IsRecording);
        Assert.Equal("Aufnahme läuft", fixture.ViewModel.StatusText);
        Assert.Equal(StopIcon, fixture.ViewModel.RecordIconSourceString);
        Assert.Equal("#FF0000", fixture.ViewModel.RecordIconColor);

        Assert.Equal(1, fixture.Location.StartListeningCalls);
        Assert.Equal(1, fixture.Dispatcher.StartTimerCalls);
        Assert.Equal(tripType.ID, fixture.Gps.CurrentTripTypeId);

        // A user exists, so no creation popup may interrupt the recording.
        Assert.DoesNotContain(nameof(UserCreationPopupViewModel), fixture.Popups.ShownPopups);
    }

    [Fact]
    public async Task StartListening_WithoutUser_AsksForUserCreationFirst()
    {
        var fixture = new Fixture(seedUser: false);
        fixture.Permissions.Status = PermissionStatus.Granted;

        await fixture.ViewModel.StartListeningCommand.ExecuteAsync(null);

        Assert.Contains(nameof(UserCreationPopupViewModel), fixture.Popups.ShownPopups);
    }

    /// <summary>
    /// Finding #4: resuming used <c>Restart()</c>, which resets the elapsed time, so pausing and
    /// continuing threw the already recorded duration away. The elapsed time must survive a pause.
    /// </summary>
    [Fact]
    public async Task StartListening_AfterPause_KeepsTheElapsedTime()
    {
        var fixture = new Fixture();
        fixture.Permissions.Status = PermissionStatus.Granted;

        await fixture.ViewModel.StartListeningCommand.ExecuteAsync(null);
        await Task.Delay(1100, TestContext.Current.CancellationToken);
        fixture.Dispatcher.RaiseTimerTick();

        var beforePause = ParseElapsed(fixture.ViewModel.StopWatchTime);
        Assert.True(beforePause >= TimeSpan.FromSeconds(1), $"Uhr lief nicht: {beforePause}.");

        // Pause: the capture is suspended but the trip stays open.
        await fixture.ViewModel.StartListeningCommand.ExecuteAsync(null);
        Assert.False(fixture.ViewModel.IsRecording);
        Assert.True(fixture.ViewModel.IsPaused);
        Assert.Equal("Pausiert", fixture.ViewModel.StatusText);

        // A pause keeps the platform session (and with it the trip and the lock-screen controls), so
        // it must not go through the full stop path.
        Assert.Equal(1, fixture.Location.PauseListeningCalls);
        Assert.Equal(0, fixture.Location.StopListeningCalls);
        Assert.Equal(0, fixture.Location.StopListeningAndDrainCalls);

        // A pause must not finalise the trip, otherwise the recorded points would be written out.
        Assert.Equal(0, fixture.Gps.FinalizeTripCalls);

        await Task.Delay(60, TestContext.Current.CancellationToken);

        // Resume.
        await fixture.ViewModel.StartListeningCommand.ExecuteAsync(null);
        fixture.Dispatcher.RaiseTimerTick();

        Assert.True(fixture.ViewModel.IsRecording);
        Assert.False(fixture.ViewModel.IsPaused);
        Assert.Equal(2, fixture.Dispatcher.StartTimerCalls);

        var afterResume = ParseElapsed(fixture.ViewModel.StopWatchTime);
        Assert.True(
            afterResume >= beforePause,
            $"Start() statt Restart(): die gemessene Zeit wurde zurueckgesetzt ({beforePause} -> {afterResume}).");
    }

    /// <summary>
    /// A full stop ends the recording, so the next one has to begin at zero again — <c>Stop()</c> alone
    /// keeps the elapsed time and the next recording would continue the previous clock.
    /// </summary>
    [Fact]
    public async Task StopListening_ResetsTheClockAndFinalizesTheTrip()
    {
        var fixture = new Fixture();
        fixture.Permissions.Status = PermissionStatus.Granted;

        await fixture.ViewModel.StartListeningCommand.ExecuteAsync(null);
        await Task.Delay(1100, TestContext.Current.CancellationToken);
        fixture.Dispatcher.RaiseTimerTick();
        Assert.NotEqual("00:00", fixture.ViewModel.StopWatchTime);

        await fixture.ViewModel.StopListeningCommand.ExecuteAsync(null);

        Assert.False(fixture.ViewModel.IsRecording);
        Assert.Equal("Bereit", fixture.ViewModel.StatusText);
        Assert.Equal("00:00", fixture.ViewModel.StopWatchTime);
        Assert.Equal("#FFFFFF", fixture.ViewModel.RecordIconColor);

        // A full stop waits for the platform drain window before the trip is closed: locations that
        // iOS still holds arrive during that window and belong to this trip.
        Assert.Equal(0, fixture.Location.StopListeningCalls);
        Assert.Equal(1, fixture.Location.StopListeningAndDrainCalls);
        Assert.Equal(1, fixture.Gps.FinalizeTripCalls);
        Assert.Equal(1, fixture.Dispatcher.StopTimerCalls);
    }

    /// <summary>
    /// Finalising only makes sense while a trip is open. Calling it unconditionally is what let a
    /// page construction end a running recording, so a stop without an open trip must stay a no-op.
    /// </summary>
    [Fact]
    public async Task StopListening_WithoutRunningTrip_DoesNotFinalize()
    {
        var fixture = new Fixture();
        fixture.Gps.IsTripInProgress = false;

        await fixture.ViewModel.StopListeningCommand.ExecuteAsync(null);

        Assert.Equal(0, fixture.Gps.FinalizeTripCalls);
        Assert.False(fixture.ViewModel.IsRecording);
    }

    /// <summary>
    /// iOS can end background updates without any error. The watchdog maps the platform snapshot to
    /// a visible warning so a silently stopped recording is no longer invisible.
    /// </summary>
    [Fact]
    public void DescribeCaptureProblem_ReportsAStoppedCapture()
    {
        var stopped = LocationCaptureHealth.Tracked(false, "Immer", 919, DateTimeOffset.Now, 1.2);

        Assert.NotNull(RecordTripTabPageViewModel.DescribeCaptureProblem(stopped));
    }

    [Fact]
    public void DescribeCaptureProblem_ReportsAFixGapLongerThanTheTolerance()
    {
        var silent = LocationCaptureHealth.Tracked(true, "Immer", 919, DateTimeOffset.Now.AddSeconds(-90), 90);

        Assert.NotNull(RecordTripTabPageViewModel.DescribeCaptureProblem(silent));
    }

    [Fact]
    public void DescribeCaptureProblem_StaysQuiet_ForAHealthyCaptureAndForUntrackedPlatforms()
    {
        var healthy = LocationCaptureHealth.Tracked(true, "Immer", 42, DateTimeOffset.Now, 1.2);

        Assert.Null(RecordTripTabPageViewModel.DescribeCaptureProblem(healthy));

        // Android and desktop report no capture details; "unbekannt" is not "gestoppt".
        Assert.Null(RecordTripTabPageViewModel.DescribeCaptureProblem(LocationCaptureHealth.Unknown));
    }

    [Fact]
    public void SelectedTripType_UpdatesStorageContextAndFloatsTheTypeToTheFavoritesFront()
    {
        var first = NewTripType("A");
        var second = NewTripType("B");
        var third = NewTripType("C");
        var fixture = new Fixture(repository => repository.Seed(first, second, third));

        // The full list keeps its stable usage order; the quick-pick chips mirror its front.
        Assert.Equal(new[] { first.ID, second.ID, third.ID }, fixture.ViewModel.TripTypes.Select(t => t.ID));
        Assert.Equal(new[] { first.ID, second.ID, third.ID }, fixture.ViewModel.FavoriteTripTypes.Select(t => t.ID));

        fixture.ViewModel.SelectedTripType = third;

        Assert.Equal(third.ID, fixture.Gps.CurrentTripTypeId);
        Assert.Equal(third.ID, fixture.ViewModel.Context.TripType?.ID);
        Assert.Equal("C", fixture.ViewModel.SelectedTripTypeName);
        Assert.Equal(new[] { third.ID, first.ID, second.ID }, fixture.ViewModel.FavoriteTripTypes.Select(t => t.ID));

        // Reordering the chips must not disturb the stable full list.
        Assert.Equal(new[] { first.ID, second.ID, third.ID }, fixture.ViewModel.TripTypes.Select(t => t.ID));

        fixture.ViewModel.SelectedTripType = null;

        Assert.Null(fixture.Gps.CurrentTripTypeId);
        Assert.Null(fixture.ViewModel.Context.TripType);
        Assert.Equal("Aktivität wählen", fixture.ViewModel.SelectedTripTypeName);
    }

    [Fact]
    public void SelectedTripType_NotInTheFavorites_IsInsertedAtTheFrontAndTrimsTheRest()
    {
        var types = new[] { NewTripType("A"), NewTripType("B"), NewTripType("C"), NewTripType("D"), NewTripType("E") };
        var fixture = new Fixture(repository => repository.Seed(types));

        Assert.Equal(types.Take(4).Select(t => t.ID), fixture.ViewModel.FavoriteTripTypes.Select(t => t.ID));

        fixture.ViewModel.SelectedTripType = types[4];

        Assert.Equal(
            new[] { types[4].ID, types[0].ID, types[1].ID, types[2].ID },
            fixture.ViewModel.FavoriteTripTypes.Select(t => t.ID));
    }

    [Theory]
    [InlineData(0, "00:00")]
    [InlineData(59_999, "00:59")]
    [InlineData(61_000, "01:01")]
    [InlineData(3_599_000, "59:59")]
    [InlineData(3_600_000, "1:00:00")]
    [InlineData(7_323_000, "2:02:03")]
    public void FormatElapsed_UsesCompactMinutesAndExpandsPastOneHour(double milliseconds, string expected)
    {
        Assert.Equal(expected, RecordTripTabPageViewModel.FormatElapsed(TimeSpan.FromMilliseconds(milliseconds)));
    }

    /// <summary>
    /// <c>LoadTripTypesAsync</c> runs fire-and-forget from the constructor. Because the fakes complete
    /// synchronously the await continues inline, so the list is ready before the constructor returns —
    /// no polling needed.
    /// </summary>
    [Fact]
    public void Constructor_LoadsTripTypesSynchronously()
    {
        var fixture = new Fixture(repository => repository.Seed(NewTripType("A"), NewTripType("B")));

        Assert.Equal(2, fixture.ViewModel.TripTypes.Count);
    }

    [Fact]
    public void Constructor_OrdersTripTypesByLastUseThenByUsageCount()
    {
        var now = DateTimeOffset.UtcNow;
        var recent = NewTripType("Zuletzt benutzt");
        var frequent = NewTripType("Haeufig, aber alt");
        var rare = NewTripType("Selten, genauso alt");
        var unused = NewTripType("Nie benutzt");

        var fixture = new Fixture(repository =>
        {
            repository.Seed(recent, frequent, rare, unused);
            repository.Seed(
                NewTrip(now, recent.ID),
                NewTrip(now.AddDays(-10), frequent.ID),
                NewTrip(now.AddDays(-11), frequent.ID),
                NewTrip(now.AddDays(-12), frequent.ID),
                NewTrip(now.AddDays(-13), frequent.ID),
                NewTrip(now.AddDays(-14), frequent.ID),
                NewTrip(now.AddDays(-10), rare.ID));
        });

        Assert.Equal(
            new[] { recent.ID, frequent.ID, rare.ID, unused.ID },
            fixture.ViewModel.TripTypes.Select(t => t.ID));

        // The most recently used type is preselected.
        Assert.Equal(recent.ID, fixture.ViewModel.SelectedTripType?.ID);
    }

    [Fact]
    public async Task IsRecording_TogglesTripTypeSelectionAndRaisesPropertyChanged()
    {
        var fixture = new Fixture();
        fixture.Permissions.Status = PermissionStatus.Granted;

        Assert.True(fixture.ViewModel.IsTripTypeSelectionVisible);

        var changed = new List<string?>();
        fixture.ViewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        await fixture.ViewModel.StartListeningCommand.ExecuteAsync(null);

        Assert.False(fixture.ViewModel.IsTripTypeSelectionVisible);
        Assert.Contains(nameof(RecordTripTabPageViewModel.IsTripTypeSelectionVisible), changed);
        Assert.Contains(nameof(RecordTripTabPageViewModel.IsRecording), changed);
    }

    /// <summary>
    /// Starting and pausing have to be told to the platform, otherwise the lock screen would keep
    /// offering "Pausieren" for a recording that is already paused (and the other way round).
    /// </summary>
    [Fact]
    public async Task StartAndPause_PublishTheStateToTheLockScreen()
    {
        var fixture = new Fixture();
        fixture.Permissions.Status = PermissionStatus.Granted;

        await fixture.ViewModel.StartListeningCommand.ExecuteAsync(null);
        Assert.Equal(RecordingRemoteState.Recording, fixture.Remote.LastState);

        await fixture.ViewModel.StartListeningCommand.ExecuteAsync(null);
        Assert.Equal(RecordingRemoteState.Paused, fixture.Remote.LastState);

        await fixture.ViewModel.StartListeningCommand.ExecuteAsync(null);
        Assert.Equal(RecordingRemoteState.Recording, fixture.Remote.LastState);

        await fixture.ViewModel.StopListeningCommand.ExecuteAsync(null);
        Assert.Equal(RecordingRemoteState.Stopped, fixture.Remote.LastState);
    }

    /// <summary>A tap on the lock screen's "Pausieren" button is the button on the page.</summary>
    [Fact]
    public async Task RemotePause_SuspendsTheRecording()
    {
        var fixture = new Fixture();
        fixture.Permissions.Status = PermissionStatus.Granted;

        await fixture.ViewModel.StartListeningCommand.ExecuteAsync(null);
        await Task.Delay(1100, TestContext.Current.CancellationToken);
        fixture.Dispatcher.RaiseTimerTick();

        var beforePause = ParseElapsed(fixture.ViewModel.StopWatchTime);

        fixture.Remote.TapPause();

        Assert.False(fixture.ViewModel.IsRecording);
        Assert.True(fixture.ViewModel.IsPaused);
        Assert.Equal("Pausiert", fixture.ViewModel.StatusText);
        Assert.Equal(PlayIcon, fixture.ViewModel.RecordIconSourceString);
        Assert.Equal(1, fixture.Location.PauseListeningCalls);

        // The pause reports its own elapsed time (the stopwatch reading, slightly ahead of the
        // rounded label), which is what the lock screen keeps showing.
        Assert.True(
            fixture.Remote.Updates[^1].Elapsed >= beforePause,
            $"Sperrbildschirm-Zeit {fixture.Remote.Updates[^1].Elapsed} < {beforePause}.");
    }

    [Fact]
    public async Task RemoteResume_ContinuesThePausedRecordingWithItsElapsedTime()
    {
        var fixture = new Fixture();
        fixture.Permissions.Status = PermissionStatus.Granted;

        await fixture.ViewModel.StartListeningCommand.ExecuteAsync(null);
        await Task.Delay(1100, TestContext.Current.CancellationToken);
        fixture.Dispatcher.RaiseTimerTick();
        var beforePause = ParseElapsed(fixture.ViewModel.StopWatchTime);

        fixture.Remote.TapPause();
        await Task.Delay(60, TestContext.Current.CancellationToken);
        fixture.Remote.TapResume();
        fixture.Dispatcher.RaiseTimerTick();

        Assert.True(fixture.ViewModel.IsRecording);
        Assert.False(fixture.ViewModel.IsPaused);
        Assert.Equal(2, fixture.Location.StartListeningCalls);
        Assert.True(ParseElapsed(fixture.ViewModel.StopWatchTime) >= beforePause);
    }

    /// <summary>A stop from the lock screen ends the trip and clears the clock like the page does.</summary>
    [Fact]
    public async Task RemoteStop_FinalizesTheTripAndResetsTheClock()
    {
        var fixture = new Fixture();
        fixture.Permissions.Status = PermissionStatus.Granted;

        await fixture.ViewModel.StartListeningCommand.ExecuteAsync(null);
        await Task.Delay(60, TestContext.Current.CancellationToken);
        fixture.Dispatcher.RaiseTimerTick();

        fixture.Remote.TapStop();

        Assert.False(fixture.ViewModel.IsRecording);
        Assert.False(fixture.ViewModel.IsPaused);
        Assert.Equal("Bereit", fixture.ViewModel.StatusText);
        Assert.Equal("00:00", fixture.ViewModel.StopWatchTime);
        Assert.Equal(1, fixture.Location.StopListeningAndDrainCalls);
        Assert.Equal(1, fixture.Gps.FinalizeTripCalls);
    }

    /// <summary>Stopping a paused recording from the lock screen is the only way out of a pause away from the app.</summary>
    [Fact]
    public async Task RemoteStop_WhilePaused_FinalizesTheTrip()
    {
        var fixture = new Fixture();
        fixture.Permissions.Status = PermissionStatus.Granted;

        await fixture.ViewModel.StartListeningCommand.ExecuteAsync(null);
        fixture.Remote.TapPause();
        fixture.Remote.TapStop();

        Assert.False(fixture.ViewModel.IsRecording);
        Assert.False(fixture.ViewModel.IsPaused);
        Assert.Equal(1, fixture.Gps.FinalizeTripCalls);
    }

    /// <summary>
    /// Buttons can arrive when they no longer fit: a second tap, or a notification that was still on
    /// screen when the recording started somewhere else. They must not start or stop the wrong thing.
    /// </summary>
    [Fact]
    public void RemoteCommands_WithoutMatchingState_DoNothing()
    {
        var fixture = new Fixture();
        fixture.Permissions.Status = PermissionStatus.Granted;

        fixture.Remote.TapPause();
        fixture.Remote.TapResume();
        fixture.Remote.TapStop();

        Assert.False(fixture.ViewModel.IsRecording);
        Assert.False(fixture.ViewModel.IsPaused);
        Assert.Equal("Bereit", fixture.ViewModel.StatusText);
        Assert.Equal(0, fixture.Location.StartListeningCalls);
        Assert.Equal(0, fixture.Location.PauseListeningCalls);
        Assert.Equal(0, fixture.Location.StopListeningAndDrainCalls);
        Assert.Empty(fixture.Remote.Updates);
    }
}

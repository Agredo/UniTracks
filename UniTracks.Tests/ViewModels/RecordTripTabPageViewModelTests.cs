using System.ComponentModel;
using UniTracks.Models.Constants;
using UniTracks.Models.Trip;
using UniTracks.Models.User;
using UniTracks.Services.ApplicationModel;
using UniTracks.Services.ApplicationModel.Permissions;
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
    /// The constructor registers a timer and calls <c>StopListening()</c> plus a fire-and-forget
    /// <c>_ = LoadTripTypesAsync()</c>, so every fake has to be wired up before it is built.
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
                Gps);
        }

        public FakeNavigationService Navigation { get; } = new();

        public FakePopupNavigationService Popups { get; } = new();

        public FakeLocationService Location { get; } = new();

        public FakePermissions Permissions { get; } = new();

        public FakeMainThread MainThread { get; } = new();

        public FakeDispatcher Dispatcher { get; } = new();

        public InMemoryRepository Repository { get; } = new();

        public FakeGpsDataStorageService Gps { get; } = new();

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

    [Fact]
    public void Constructor_StartsIdleAndConfiguresTheClock()
    {
        var fixture = new Fixture();

        Assert.False(fixture.ViewModel.IsRecording);
        Assert.Equal("Bereit", fixture.ViewModel.StatusText);
        Assert.Equal("00:00:000", fixture.ViewModel.StopWatchTime);
        Assert.Equal(PlayIcon, fixture.ViewModel.RecordIconSourceString);
        Assert.Equal("#FFFFFF", fixture.ViewModel.RecordIconColor);

        Assert.Equal(1, fixture.Dispatcher.CreateTimerCalls);
        Assert.Equal(TimeSpan.FromMilliseconds(100), fixture.Dispatcher.TimerInterval!.Value);

        // The constructor runs a full stop once, which is also what finalises a trip left over from
        // a previous session.
        Assert.Equal(1, fixture.Location.StopListeningCalls);
        Assert.Equal(1, fixture.Gps.FinalizeTripCalls);
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
        await Task.Delay(120, TestContext.Current.CancellationToken);
        fixture.Dispatcher.RaiseTimerTick();

        var beforePause = TimeSpan.Parse(fixture.ViewModel.StopWatchTime);
        Assert.True(beforePause >= TimeSpan.FromMilliseconds(100), $"Uhr lief nicht: {beforePause}.");

        // Pause. The constructor already stopped once, so this is the second stop.
        await fixture.ViewModel.StartListeningCommand.ExecuteAsync(null);
        Assert.False(fixture.ViewModel.IsRecording);
        Assert.Equal("Pausiert", fixture.ViewModel.StatusText);
        Assert.Equal(2, fixture.Location.StopListeningCalls);

        // A pause must not finalise the trip, otherwise the recorded points would be written out.
        Assert.Equal(1, fixture.Gps.FinalizeTripCalls);

        await Task.Delay(60, TestContext.Current.CancellationToken);

        // Resume.
        await fixture.ViewModel.StartListeningCommand.ExecuteAsync(null);
        fixture.Dispatcher.RaiseTimerTick();

        Assert.True(fixture.ViewModel.IsRecording);
        Assert.Equal(2, fixture.Dispatcher.StartTimerCalls);

        var afterResume = TimeSpan.Parse(fixture.ViewModel.StopWatchTime);
        Assert.True(
            afterResume >= beforePause,
            $"Start() statt Restart(): die gemessene Zeit wurde zurueckgesetzt ({beforePause} -> {afterResume}).");
    }

    /// <summary>
    /// A full stop ends the recording, so the next one has to begin at zero again — <c>Stop()</c> alone
    /// keeps the elapsed time and the next recording would continue the previous clock.
    ///
    /// Note: the reset literal <c>"00:00:000"</c> does not match the <c>hh\:mm\:ss\.fff</c> format the
    /// timer produces (<c>"00:00:00.000"</c>), so the label briefly shows a malformed value until the
    /// next tick. Recorded as observed behaviour; production is unchanged.
    /// </summary>
    [Fact]
    public async Task StopListening_ResetsTheClockAndFinalizesTheTrip()
    {
        var fixture = new Fixture();
        fixture.Permissions.Status = PermissionStatus.Granted;

        await fixture.ViewModel.StartListeningCommand.ExecuteAsync(null);
        await Task.Delay(60, TestContext.Current.CancellationToken);
        fixture.Dispatcher.RaiseTimerTick();
        Assert.NotEqual("00:00:000", fixture.ViewModel.StopWatchTime);

        fixture.ViewModel.StopListeningCommand.Execute(null);

        Assert.False(fixture.ViewModel.IsRecording);
        Assert.Equal("Bereit", fixture.ViewModel.StatusText);
        Assert.Equal("00:00:000", fixture.ViewModel.StopWatchTime);
        Assert.Equal("#FFFFFF", fixture.ViewModel.RecordIconColor);

        Assert.Equal(2, fixture.Location.StopListeningCalls);
        Assert.Equal(2, fixture.Gps.FinalizeTripCalls);
        Assert.Equal(2, fixture.Dispatcher.StopTimerCalls);
    }

    [Fact]
    public void SelectedTripType_UpdatesStorageAndMovesTheTypeToTheFront()
    {
        var first = NewTripType("A");
        var second = NewTripType("B");
        var third = NewTripType("C");
        var fixture = new Fixture(repository => repository.Seed(first, second, third));

        Assert.Equal(new[] { first.ID, second.ID, third.ID }, fixture.ViewModel.TripTypes.Select(t => t.ID));

        fixture.ViewModel.SelectedTripType = third;

        Assert.Equal(third.ID, fixture.Gps.CurrentTripTypeId);
        Assert.Equal(new[] { third.ID, first.ID, second.ID }, fixture.ViewModel.TripTypes.Select(t => t.ID));

        fixture.ViewModel.SelectedTripType = null;

        Assert.Null(fixture.Gps.CurrentTripTypeId);
        Assert.Equal(new[] { third.ID, first.ID, second.ID }, fixture.ViewModel.TripTypes.Select(t => t.ID));
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
}

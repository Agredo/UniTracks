using UniTracks.Models.Trip;
using UniTracks.Tests.ViewModels.Fakes;
using UniTracks.ViewModels.Pages.Tabs;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.Tests.ViewModels;

/// <summary>
/// Tests for <see cref="TripTabPageViewModel"/>. The interesting part is <c>DeleteTripAsync</c>:
/// a trip owns its GPS points and the store has no cascade delete for them, so the points have to be
/// removed explicitly — and before the trip (finding #7).
/// </summary>
public sealed class TripTabPageViewModelTests
{
    private sealed class Fixture
    {
        public Fixture(Action<InMemoryRepository>? seed = null)
        {
            // The constructor loads the trips, so the seed has to be in place beforehand.
            seed?.Invoke(Repository);

            ViewModel = new TripTabPageViewModel(Navigation, Popups, Location, FileSystem, Gps, Repository);
        }

        public FakeNavigationService Navigation { get; } = new();

        public FakePopupNavigationService Popups { get; } = new();

        public FakeLocationService Location { get; } = new();

        public FakeFileSystem FileSystem { get; } = new();

        public FakeGpsDataStorageService Gps { get; } = new();

        public InMemoryRepository Repository { get; } = new();

        public TripTabPageViewModel ViewModel { get; }
    }

    private static LocationModel NewLocation(Guid tripId, double latitude) => new()
    {
        ID = Guid.NewGuid(),
        TripID = tripId,
        Latitude = latitude,
        Timestamp = DateTimeOffset.UtcNow,
    };

    /// <summary>Builds a trip together with <paramref name="locationCount"/> GPS points that point back at it.</summary>
    private static Trip NewTrip(string name, DateTimeOffset startTime, int locationCount = 0)
    {
        var trip = new Trip
        {
            ID = Guid.NewGuid(),
            Name = name,
            StartTime = startTime,
            EndTime = startTime.AddHours(1),
            Locations = new(),
        };

        for (var i = 0; i < locationCount; i++)
        {
            trip.Locations.Add(NewLocation(trip.ID, 50 + i));
        }

        return trip;
    }

    /// <summary>
    /// Seeds trips <em>and</em> their GPS points. <c>DeleteTripAsync</c> looks the points up in the
    /// repository, so putting them only on <c>trip.Locations</c> would not be enough.
    /// </summary>
    private static void SeedTrips(InMemoryRepository repository, params Trip[] trips)
    {
        repository.Seed(trips);
        repository.Seed(trips.SelectMany(trip => trip.Locations).ToArray());
    }

    [Fact]
    public void Constructor_LoadsTripsNewestFirst()
    {
        var now = DateTimeOffset.UtcNow;
        var oldest = NewTrip("Aeltester", now.AddDays(-2));
        var middle = NewTrip("Mittlerer", now.AddDays(-1));
        var newest = NewTrip("Neuester", now);

        var fixture = new Fixture(repository => SeedTrips(repository, oldest, middle, newest));

        Assert.Equal(
            new[] { newest.ID, middle.ID, oldest.ID },
            fixture.ViewModel.Trips.Select(trip => trip.ID));

        Assert.Equal("test://unitracks-in-memory", fixture.ViewModel.DatabasePath);
    }

    /// <summary>
    /// The map is filled from <c>Trips.Last()</c>. Because the list is sorted newest-first, <c>Last()</c>
    /// is the <em>oldest</em> trip — so the map shows the oldest tour, not the most recent one.
    /// Surprising, but that is the current behaviour and the test pins it down.
    /// </summary>
    [Fact]
    public void Constructor_ShowsTheLocationsOfTheOldestTrip()
    {
        var now = DateTimeOffset.UtcNow;
        var oldest = NewTrip("Aeltester", now.AddDays(-2), locationCount: 2);
        var newest = NewTrip("Neuester", now, locationCount: 5);

        var fixture = new Fixture(repository => SeedTrips(repository, oldest, newest));

        Assert.Equal(
            oldest.Locations.Select(location => location.ID),
            fixture.ViewModel.Locations.Select(location => location.ID));
    }

    /// <summary>
    /// Finding #7: the points must be deleted before the trip. Deleting the trip first would leave its
    /// points behind — the foreign key is only set to NULL, not cascaded — so the database grew with
    /// every deleted trip.
    /// </summary>
    [Fact]
    public async Task DeleteTripAsync_RemovesTheLocationsBeforeTheTrip()
    {
        var now = DateTimeOffset.UtcNow;
        var doomed = NewTrip("Zu loeschen", now, locationCount: 3);
        var survivor = NewTrip("Bleibt", now.AddDays(-1), locationCount: 1);

        var fixture = new Fixture(repository => SeedTrips(repository, doomed, survivor));

        await fixture.ViewModel.DeleteTripAsync(doomed);

        var writes = fixture.Repository.Calls
            .Where(call => call.Operation is "DeleteRange" or "Delete")
            .ToList();

        Assert.Equal(2, writes.Count);
        Assert.Equal("DeleteRange", writes[0].Operation);
        Assert.Equal(typeof(LocationModel), writes[0].EntityType);
        Assert.Equal("Delete", writes[1].Operation);
        Assert.Equal(typeof(Trip), writes[1].EntityType);

        // State check: only the deleted trip's points are gone, the other trip keeps its point.
        var remaining = fixture.Repository.Rows<LocationModel>();
        Assert.Single(remaining);
        Assert.Equal(survivor.ID, remaining[0].TripID);

        Assert.DoesNotContain(fixture.ViewModel.Trips, trip => trip.ID == doomed.ID);
        Assert.Contains(fixture.ViewModel.Trips, trip => trip.ID == survivor.ID);
    }

    [Fact]
    public async Task DeleteTripAsync_WithoutLocations_DoesNotCallDeleteRange()
    {
        var trip = NewTrip("Ohne Punkte", DateTimeOffset.UtcNow);
        var fixture = new Fixture(repository => SeedTrips(repository, trip));

        await fixture.ViewModel.DeleteTripAsync(trip);

        Assert.DoesNotContain(fixture.Repository.Calls, call => call.Operation == "DeleteRange");
        Assert.Contains(fixture.Repository.Calls, call => call.Operation == "Delete");
        Assert.Empty(fixture.ViewModel.Trips);
    }

    /// <summary>
    /// Known gap, recorded as-is: <c>GetTrips()</c> only clears <c>Locations</c> inside the
    /// <c>if (Trips.Count &gt; 0)</c> branch, so after removing the <em>last</em> trip the map keeps
    /// showing the points of the trip that no longer exists. Production is unchanged.
    /// </summary>
    [Fact]
    public async Task DeleteTripAsync_ForTheLastTrip_LeavesTheOldLocationsOnTheMap()
    {
        var trip = NewTrip("Einziger", DateTimeOffset.UtcNow, locationCount: 2);
        var fixture = new Fixture(repository => SeedTrips(repository, trip));

        Assert.Equal(2, fixture.ViewModel.Locations.Count);

        await fixture.ViewModel.DeleteTripAsync(trip);

        Assert.Empty(fixture.ViewModel.Trips);
        Assert.Equal(2, fixture.ViewModel.Locations.Count);
        Assert.Empty(fixture.Repository.Rows<LocationModel>());
    }

    [Fact]
    public async Task RenameTripAsync_UpdatesTheNameAndReloads()
    {
        var trip = NewTrip("Alter Name", DateTimeOffset.UtcNow);
        var fixture = new Fixture(repository => SeedTrips(repository, trip));

        await fixture.ViewModel.RenameTripAsync(trip, "Neuer Name");

        Assert.Equal("Neuer Name", trip.Name);
        Assert.Equal("Neuer Name", Assert.Single(fixture.ViewModel.Trips).Name);
        Assert.Contains(fixture.Repository.Calls, call => call.Operation == "Update");
    }

    [Fact]
    public void SelectedTrip_WithATrip_NavigatesToTheTripOverviewPage()
    {
        var trip = NewTrip("Ziel", DateTimeOffset.UtcNow);
        var fixture = new Fixture(repository => SeedTrips(repository, trip));

        fixture.ViewModel.SelectedTrip = trip;

        var navigation = Assert.Single(fixture.Navigation.Navigations);
        Assert.Equal("TripOverviewPage", navigation.Route);
        Assert.Equal(trip, navigation.Parameters!["parameter"]);
    }

    [Fact]
    public void SelectedTrip_WithNull_DoesNotNavigate()
    {
        var fixture = new Fixture();

        fixture.ViewModel.SelectedTrip = null;

        Assert.Empty(fixture.Navigation.Navigations);
    }

    [Fact]
    public async Task Refresh_ReloadsTheTripsAndHidesTheIndicator()
    {
        var fixture = new Fixture();
        Assert.Empty(fixture.ViewModel.Trips);

        fixture.ViewModel.RefreshIndicatorVisible = true;
        fixture.Repository.Seed(NewTrip("Nachgeladen", DateTimeOffset.UtcNow));

        await fixture.ViewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Single(fixture.ViewModel.Trips);
        Assert.False(fixture.ViewModel.RefreshIndicatorVisible);
    }
}

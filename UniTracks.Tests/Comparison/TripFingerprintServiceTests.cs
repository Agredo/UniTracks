using UniTracks.Models.Comparison;
using UniTracks.Models.Trip;
using UniTracks.Services.Comparison;
using UniTracks.Tests.TestSupport;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.Tests.Comparison;

/// <summary>
/// Exercises the fingerprint against the real schema. The test database runs the migrations, so this
/// also proves the <c>TripFingerprints</c> table — including the trip type columns — is created.
/// </summary>
public class TripFingerprintServiceTests
{
    [Fact]
    public async Task Backfill_CreatesFingerprintsForTripsWithoutLocationsInTheGraph()
    {
        using var database = new SqliteTestDatabase();
        var run = ComparisonFixtures.Type("run", "running");

        await database.Repository.Add(run);

        var tripId = Guid.NewGuid();
        var trip = ComparisonFixtures.Trip(
            tripId,
            "morgenlauf",
            ComparisonFixtures.StraightTrack(tripId, 50.0, 8.0, 5000),
            run,
            distanceMeters: 5000,
            movingSeconds: 1500);

        await AddTripWithLocationsAsync(database, trip);

        var service = CreateService(database);

        int rebuilt = await service.BackfillAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, rebuilt);

        var stored = await service.GetAsync(tripId);
        Assert.NotNull(stored);
        Assert.Equal("running", stored!.TripCategory);
        Assert.Equal("run", stored.TripIdentifier);
        Assert.Equal(5000, stored.DistanceMeters, 1);
        Assert.Equal(32, stored.PolylineLatitudes.Length);
        Assert.NotEmpty(stored.RouteCells);
    }

    [Fact]
    public async Task Backfill_IsIdempotent()
    {
        using var database = new SqliteTestDatabase();
        var tripId = Guid.NewGuid();
        var trip = ComparisonFixtures.Trip(
            tripId,
            "morgenlauf",
            ComparisonFixtures.StraightTrack(tripId, 50.0, 8.0, 4000),
            distanceMeters: 4000,
            movingSeconds: 1200);

        await AddTripWithLocationsAsync(database, trip);

        var service = CreateService(database);

        Assert.Equal(1, await service.BackfillAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await service.BackfillAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Ensure_ReturnsStoredFingerprintWithoutRebuilding()
    {
        using var database = new SqliteTestDatabase();
        var tripId = Guid.NewGuid();
        var trip = ComparisonFixtures.Trip(
            tripId,
            "morgenlauf",
            ComparisonFixtures.StraightTrack(tripId, 50.0, 8.0, 4000),
            distanceMeters: 4000,
            movingSeconds: 1200);

        await AddTripWithLocationsAsync(database, trip);

        var service = CreateService(database);

        var first = await service.EnsureAsync(trip);
        var second = await service.EnsureAsync(trip);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.TripID, second!.TripID);
    }

    [Fact]
    public async Task Remove_DropsTheFingerprint()
    {
        using var database = new SqliteTestDatabase();
        var tripId = Guid.NewGuid();
        var trip = ComparisonFixtures.Trip(
            tripId,
            "morgenlauf",
            ComparisonFixtures.StraightTrack(tripId, 50.0, 8.0, 4000),
            distanceMeters: 4000,
            movingSeconds: 1200);

        await AddTripWithLocationsAsync(database, trip);

        var service = CreateService(database);
        await service.EnsureAsync(trip);

        await service.RemoveAsync(tripId);

        Assert.Null(await service.GetAsync(tripId));
    }

    [Fact]
    public async Task Fingerprint_SurvivesARoundTripIncludingTheArrays()
    {
        using var database = new SqliteTestDatabase();
        var walk = ComparisonFixtures.Type("walk", "running");
        await database.Repository.Add(walk);

        var tripId = Guid.NewGuid();
        var trip = ComparisonFixtures.Trip(
            tripId,
            "spaziergang",
            ComparisonFixtures.StraightTrack(tripId, 48.1, 11.5, 2500),
            walk,
            distanceMeters: 2500,
            movingSeconds: 1800);

        await AddTripWithLocationsAsync(database, trip);

        var service = CreateService(database);
        var built = await service.EnsureAsync(trip);

        Assert.NotNull(built);

        // Detach everything so the assertion reads the persisted row and not the tracked instance.
        database.Context.ChangeTracker.Clear();

        var reloaded = await service.GetAsync(tripId);

        Assert.NotNull(reloaded);
        Assert.Equal(built!.PolylineLatitudes, reloaded!.PolylineLatitudes);
        Assert.Equal(built.PolylineLongitudes, reloaded.PolylineLongitudes);
        Assert.Equal(built.RouteCells, reloaded.RouteCells);
        Assert.Equal("running", reloaded.TripCategory);
        Assert.Equal("walk", reloaded.TripIdentifier);
    }

    [Fact]
    public async Task DeletingATrip_CascadesToItsFingerprint()
    {
        using var database = new SqliteTestDatabase();
        var tripId = Guid.NewGuid();
        var trip = ComparisonFixtures.Trip(
            tripId,
            "morgenlauf",
            ComparisonFixtures.StraightTrack(tripId, 50.0, 8.0, 4000),
            distanceMeters: 4000,
            movingSeconds: 1200);

        await AddTripWithLocationsAsync(database, trip);

        var service = CreateService(database);
        await service.EnsureAsync(trip);

        var storedTrip = await database.Repository.GetByIdAsync<Models.Trip.Trip>(tripId);
        await database.Repository.Delete(storedTrip!);

        Assert.Null(await service.GetAsync(tripId));
    }

    [Fact]
    public async Task Backfill_HealsAStoreThatHoldsSeveralFingerprintsForOneTrip()
    {
        // Regression: LiteDB ignored the [Key] on TripID, so every index pass appended another row
        // for the same trip (18 trips came to 36 rows) and the next backfill threw on the duplicate
        // keys. The real stores can no longer produce that state, hence the permissive fake.
        var repository = new InMemoryRepository();
        var tripId = Guid.NewGuid();
        var trip = ComparisonFixtures.Trip(
            tripId,
            "morgenlauf",
            ComparisonFixtures.StraightTrack(tripId, 50.0, 8.0, 4000),
            distanceMeters: 4000,
            movingSeconds: 1200);

        repository.Seed(new Trip
        {
            ID = trip.ID,
            Name = trip.Name,
            StartTime = trip.StartTime,
            EndTime = trip.EndTime,
            Distance = trip.Distance,
            MovingTime = trip.MovingTime,
        });

        foreach (LocationModel location in trip.Locations)
        {
            repository.Seed(location);
        }

        var current = new TripFingerprint
        {
            TripID = tripId,
            Version = TripFingerprintBuilder.Version,
            DistanceMeters = 4000,
        };

        var surplus = new TripFingerprint
        {
            TripID = tripId,
            Version = TripFingerprintBuilder.Version,
            DistanceMeters = 4000,
        };

        repository.Seed(current, surplus);

        var service = new TripFingerprintService(repository, new TripTypeCatalog(repository));

        await service.BackfillAsync(TestContext.Current.CancellationToken);

        // The duplicate is dropped and the surviving row is the one the comparison reads.
        Assert.Same(surplus, Assert.Single(repository.Deleted));
        Assert.Single(await service.GetAllAsync());
        Assert.Equal(4000, (await service.GetAsync(tripId))!.DistanceMeters, 1);
    }

    [Fact]
    public async Task GetAllAsync_CollapsesDuplicateRowsEvenWithoutABackfill()
    {
        var repository = new InMemoryRepository();
        var tripId = Guid.NewGuid();

        repository.Seed(
            new TripFingerprint { TripID = tripId, Version = 1, DistanceMeters = 1000 },
            new TripFingerprint { TripID = tripId, Version = 2, DistanceMeters = 2000 },
            new TripFingerprint { TripID = Guid.NewGuid(), Version = 2, DistanceMeters = 3000 });

        var service = new TripFingerprintService(repository, new TripTypeCatalog(repository));

        var all = await service.GetAllAsync();

        Assert.Equal(2, all.Select(fingerprint => fingerprint.TripID).Distinct().Count());
        Assert.Equal(2000, all.Single(fingerprint => fingerprint.TripID == tripId).DistanceMeters);
    }

    private static TripFingerprintService CreateService(SqliteTestDatabase database) =>
        new(database.Repository, new TripTypeCatalog(database.Repository));    private static async Task AddTripWithLocationsAsync(SqliteTestDatabase database, Models.Trip.Trip trip)
    {
        var locations = trip.Locations;

        await database.Repository.Add(new Models.Trip.Trip
        {
            ID = trip.ID,
            Name = trip.Name,
            StartTime = trip.StartTime,
            EndTime = trip.EndTime,
            Distance = trip.Distance,
            MovingTime = trip.MovingTime,
            TripTypeId = trip.TripTypeId,
        });

        foreach (LocationModel location in locations)
        {
            await database.Repository.Add(location);
        }
    }
}

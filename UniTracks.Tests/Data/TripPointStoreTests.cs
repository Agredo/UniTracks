using UniTracks.Data.LiteDB;
using UniTracks.Data.Repository;
using UniTracks.Models.Trip;
using LiteDatabase = UniTracks.Data.LiteDB.LiteDatabase;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.Tests.Data;

/// <summary>
/// Covers the split that keeps the trip list cheap: on LiteDB a trip's GPS points live in the
/// <c>Location</c> collection instead of inside the trip document, and <see cref="TripPointStore"/>
/// is the only place that knows about it.
/// </summary>
public sealed class TripPointStoreTests : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"unitracks-points-{Guid.NewGuid():N}.litedb");
    private readonly LiteDatabase database;
    private readonly LiteDbRepository repository;

    public TripPointStoreTests()
    {
        database = new LiteDatabase(path);
        repository = new LiteDbRepository(database);
    }

    public void Dispose()
    {
        database.Database.Dispose();

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // A leftover temp file must never fail a test run.
        }
    }

    private static Trip Trip(params DateTimeOffset[] timestamps) => new()
    {
        ID = Guid.NewGuid(),
        Name = "Abendlauf",
        StartTime = new DateTimeOffset(2026, 3, 1, 18, 0, 0, TimeSpan.Zero),
        EndTime = new DateTimeOffset(2026, 3, 1, 19, 0, 0, TimeSpan.Zero),
        Locations = timestamps
            .Select((timestamp, index) => new LocationModel
            {
                ID = Guid.NewGuid(),
                Latitude = 48.1 + index,
                Longitude = 11.5 + index,
                Timestamp = timestamp,
            })
            .ToList(),
    };

    private static DateTimeOffset At(int minute) =>
        new DateTimeOffset(2026, 3, 1, 18, minute, 0, TimeSpan.Zero);

    private global::LiteDB.ILiteCollection<Trip> TripDocuments => database.Database.GetCollection<Trip>();

    [Fact]
    public async Task Add_StoresThePointsOutsideTheTripDocument()
    {
        var trip = Trip(At(0), At(1), At(2));

        await repository.Add(trip);

        // The document itself carries none of them any more - that is what makes the list read cheap.
        Assert.Empty(TripDocuments.FindById(trip.ID)!.Locations);
        Assert.Equal(3, database.Locations.Count());

        // Whoever needs the points still gets them, oldest first.
        var loaded = Assert.Single(await repository.GetAllAsync<Trip>(t => t.Locations));
        Assert.Equal(new[] { At(0), At(1), At(2) }, loaded.Locations.Select(point => point.Timestamp));
        Assert.All(loaded.Locations, point => Assert.Equal(trip.ID, point.TripID));
    }

    [Fact]
    public async Task ListRead_WithoutTheInclude_LeavesThePointsOut()
    {
        await repository.Add(Trip(At(0), At(1), At(2)));

        var listed = Assert.Single(await repository.GetAllAsync<Trip>(t => t.TripType!));

        Assert.Empty(listed.Locations);
    }

    /// <summary>
    /// The recording writes one GPS point per fix while the trip is being recorded. Those writes used
    /// to invalidate the cached trip list, so the next visit of the list paid for a full re-read.
    /// </summary>
    [Fact]
    public async Task PointWrite_DoesNotInvalidateTheCachedTripList()
    {
        var trip = Trip(At(0));
        await repository.Add(trip);

        // Prime the cache with a read that does not ask for points.
        Assert.Equal("Abendlauf", Assert.Single(await repository.GetAllAsync<Trip>()).Name);

        // Changed behind the repository's back: a cache hit hands out the value it read before, a
        // cache miss would return the new one. This is the only way to tell the two apart from
        // outside, and it is exactly what the invalidation rule has to get right.
        TripDocuments.Update(trip with { Name = "Direkt geschrieben" });

        await repository.Add(new LocationModel
        {
            ID = Guid.NewGuid(),
            TripID = trip.ID,
            Timestamp = At(1),
        });

        Assert.Equal("Abendlauf", Assert.Single(await repository.GetAllAsync<Trip>()).Name);
    }

    [Fact]
    public async Task TripWrite_InvalidatesTheCachedTripList()
    {
        var trip = Trip(At(0));
        await repository.Add(trip);
        Assert.Single(await repository.GetAllAsync<Trip>());

        await repository.Update(trip with { Name = "Neuer Name" });

        Assert.Equal("Neuer Name", Assert.Single(await repository.GetAllAsync<Trip>()).Name);
    }

    /// <summary>
    /// The list hands the trip over without its points and without its type; the edit popup writes it
    /// back the same way. The write must not drop what it simply did not load.
    /// </summary>
    [Fact]
    public async Task Update_KeepsTheStoredTypeAndThePoints()
    {
        var type = new TripType
        {
            ID = Guid.NewGuid(),
            Name = "Laufen",
            Identifier = "run",
            Description = "Laufen",
            Category = "Cardio",
        };
        var trip = Trip(At(0), At(1)) with { TripTypeId = type.ID, TripType = type };

        await repository.Add(type);
        await repository.Add(trip);

        var listed = Assert.Single(await repository.GetAllAsync<Trip>(t => t.TripType!));
        listed.Name = "Umbenannt";
        listed.TripType = null;
        await repository.Update(listed);

        var stored = Assert.Single(await repository.GetAllAsync<Trip>(t => t.TripType!, t => t.Locations));
        Assert.Equal("Umbenannt", stored.Name);
        Assert.Equal(type.ID, stored.TripTypeId);
        Assert.Equal("Laufen", stored.TripType?.Name);
        Assert.Equal(2, stored.Locations.Count);
    }

    [Fact]
    public async Task Migration_MovesEmbeddedPointsOver_AndCanRunAgain()
    {
        // A document written by a build that still embedded the points.
        var trip = Trip(At(0), At(1), At(2));
        TripDocuments.Insert(trip);

        var pointStore = new TripPointStore(database);
        await pointStore.RunAsync();

        Assert.Empty(TripDocuments.FindById(trip.ID)!.Locations);
        Assert.Equal(3, database.Locations.Count());
        Assert.Equal(3, Assert.Single(await repository.GetAllAsync<Trip>(t => t.Locations)).Locations.Count);

        // Starting the app again must not lose or duplicate anything.
        await pointStore.RunAsync();

        Assert.Equal(3, database.Locations.Count());
        Assert.Equal(3, Assert.Single(await repository.GetAllAsync<Trip>(t => t.Locations)).Locations.Count);
    }

    [Fact]
    public async Task Delete_RemovesThePointsOfTheTrip()
    {
        var trip = Trip(At(0), At(1));
        await repository.Add(trip);

        await repository.Delete(trip);

        Assert.Equal(0, database.Locations.Count());
        Assert.Empty(await repository.GetAllAsync<Trip>());
    }
}

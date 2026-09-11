using System.Collections.Concurrent;
using UniTracks.Data.Repository;
using UniTracks.Models.GPS;
using UniTracks.Services.Data;
using UniTracks.Services.Location;
using UniTracks.Tests.TestSupport;
using LocationModel = UniTracks.Models.Location.Location;
using TripModel = UniTracks.Models.Trip.Trip;
using TripTypeModel = UniTracks.Models.Trip.TripType;

namespace UniTracks.Tests.GpsStorage;

/// <summary>
/// Regression tests for the second critical finding: <c>GpsDataStorageService</c> used to touch the
/// in-flight <c>currentTrip</c> after it had already released <c>storeGate</c>. A concurrent
/// <see cref="GpsDataStorageService.FinalizeTrip"/> nulls that field, so the background location
/// callback crashed with a <see cref="NullReferenceException"/>. The service is exercised against a
/// real SQLite repository because the ordering of <c>Add(location)</c> before <c>Update(trip)</c>
/// is part of the contract.
/// </summary>
public sealed class GpsDataStorageServiceTests
{
    private static readonly DateTimeOffset Epoch = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// A track that heads east with a small alternating lateral jitter, so the raw point-to-point
    /// distance is measurably longer than the smoothed one.
    /// </summary>
    private static GPSInformatoion Point(int index) => new(
        new Position(8.0 + (0.0003 * index), 50.0 + (index % 2 == 0 ? 0.0 : 0.0001)),
        PositionAccuracy: 5.0,
        Timestamp: Epoch.AddSeconds(3 * index),
        Heading: 90,
        HeadingAccuracy: 1,
        Altitude: 100,
        Speed: 3.0,
        SpeedAccuracy: 0.5);

    private static async Task<GpsDataStorageService> FeedAsync(
        SqliteTestDatabase db,
        int pointCount,
        Guid? tripTypeId = null)
    {
        var service = new GpsDataStorageService(db.Repository) { CurrentTripTypeId = tripTypeId };

        for (var index = 0; index < pointCount; index++)
        {
            await service.StoreData(Point(index));
        }

        return service;
    }

    private static List<LocationModel> StoredLocations(IRepository repository) =>
        repository.Get<LocationModel>().OrderBy(location => location.Timestamp).ToList();

    [Fact]
    public async Task StoreData_FirstPoint_CreatesATripOwningThatLocation()
    {
        using var db = new SqliteTestDatabase();
        // Trip.TripTypeId is a real FK onto the migration-seeded TripTypes catalog.
        var tripTypeId = db.Repository.Get<TripTypeModel>().First().ID;

        await FeedAsync(db, 1, tripTypeId);

        var trip = Assert.Single(db.Repository.Get<TripModel>());
        Assert.Equal<Guid?>(tripTypeId, trip.TripTypeId);
        Assert.Equal(0.0, trip.Distance);

        var location = Assert.Single(StoredLocations(db.Repository));
        Assert.Equal(trip.ID, location.TripID);
        Assert.Equal(50.0, location.Latitude, 9);
        Assert.Equal(8.0, location.Longitude, 9);
    }

    [Fact]
    public async Task StoreData_FurtherPoints_AccumulateOnTheSameTrip()
    {
        using var db = new SqliteTestDatabase();

        await FeedAsync(db, 5);

        var trip = Assert.Single(db.Repository.Get<TripModel>());
        var locations = StoredLocations(db.Repository);

        Assert.Equal(5, locations.Count);
        Assert.All(locations, location => Assert.Equal(trip.ID, location.TripID));
        Assert.True(trip.Distance > 0, "Die Distanz muss aus den GPS-Punkten aufsummiert werden.");
        Assert.Equal(3.0, trip.AverageSpeed);
        Assert.Equal(3.0, trip.MaxSpeed);
        Assert.Equal(3.0, trip.MinSpeed);
        Assert.Equal(100.0, trip.MaxAltitude);
        Assert.True(trip.EndTime >= trip.StartTime);
    }

    [Fact]
    public async Task FinalizeTrip_WithMoreThanTwoLocations_ReplacesRawDistanceWithSmoothedDistance()
    {
        using var db = new SqliteTestDatabase();
        var service = await FeedAsync(db, 5);

        var rawDistance = Assert.Single(db.Repository.Get<TripModel>()).Distance;
        var points = StoredLocations(db.Repository);

        service.FinalizeTrip();

        var smoothedDistance = Assert.Single(db.Repository.Get<TripModel>()).Distance;

        Assert.True(rawDistance > 0);
        Assert.True(
            smoothedDistance < rawDistance,
            $"Die geglättete Distanz ({smoothedDistance}) muss die verrauschte Rohdistanz ({rawDistance}) unterschreiten.");
        Assert.Equal(TrackSmoother.SmoothedDistanceMeters(points), smoothedDistance!.Value, 6);
    }

    [Fact]
    public async Task FinalizeTrip_WithTwoOrFewerLocations_KeepsTheRawDistance()
    {
        using var db = new SqliteTestDatabase();
        var service = await FeedAsync(db, 2);

        var rawDistance = Assert.Single(db.Repository.Get<TripModel>()).Distance;

        service.FinalizeTrip();

        // Smoothing needs at least three points, so the incremental value has to survive.
        Assert.Equal(rawDistance, Assert.Single(db.Repository.Get<TripModel>()).Distance);
    }

    [Fact]
    public async Task FinalizeTrip_ThenStoreData_StartsAFreshTrip()
    {
        using var db = new SqliteTestDatabase();
        var service = await FeedAsync(db, 3);

        service.FinalizeTrip();
        await service.StoreData(Point(3));

        var trips = db.Repository.Get<TripModel>().ToList();
        var locations = StoredLocations(db.Repository);

        Assert.Equal(2, trips.Count);
        Assert.Contains(trips, trip => locations.Count(location => location.TripID == trip.ID) == 3);
        Assert.Contains(trips, trip => locations.Count(location => location.TripID == trip.ID) == 1);
    }

    /// <summary>
    /// Serialization smoke test for the finding: <see cref="GpsDataStorageService.StoreData"/> and
    /// <see cref="GpsDataStorageService.FinalizeTrip"/> are entered from different threads (the UI
    /// thread and the background location callback) and must not interleave.
    /// <para>
    /// Note the limit of this test. Removing <c>storeGate</c> makes it fail with a
    /// <c>DbUpdateConcurrencyException</c>, but the narrow original race — reading <c>currentTrip</c>
    /// in the few instructions between <c>storeGate.Release()</c> and the end of the method, after
    /// <see cref="GpsDataStorageService.FinalizeTrip"/> had already nulled it — is not reproducible
    /// from outside the class: it needs the reader to lose the CPU between two adjacent statements.
    /// Even 900 concurrent operations driven against a finalizer thread spinning in a tight loop did
    /// not trigger it. That part of the fix therefore rests on the code shape (no <c>currentTrip</c>
    /// access outside the gate), which is asserted by review rather than by this test.
    /// </para>
    /// </summary>
    [Fact]
    public async Task StoreData_ConcurrentWithFinalizeTrip_DoesNotThrow()
    {
        using var db = new SqliteTestDatabase();
        var service = new GpsDataStorageService(db.Repository);
        var failures = new ConcurrentBag<Exception>();
        var cancellationToken = TestContext.Current.CancellationToken;

        var writers = Enumerable.Range(0, 4).Select(worker => Task.Run(async () =>
        {
            for (var index = 0; index < 15; index++)
            {
                try
                {
                    await service.StoreData(Point((worker * 15) + index));
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
        }, cancellationToken)).ToList();

        var finalizer = Task.Run(() =>
        {
            for (var index = 0; index < 15; index++)
            {
                try
                {
                    service.FinalizeTrip();
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
        }, cancellationToken);

        await Task.WhenAll(writers.Append(finalizer));

        Assert.True(
            failures.IsEmpty,
            string.Join(Environment.NewLine, failures.Select(failure => failure.ToString())));
    }
}

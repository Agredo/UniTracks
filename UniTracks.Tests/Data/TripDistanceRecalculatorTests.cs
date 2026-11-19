using UniTracks.Models.Trip;
using UniTracks.Services.Data;
using UniTracks.Tests.TestSupport;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.Tests.Data;

public class TripDistanceRecalculatorTests
{
    [Fact]
    public async Task RecalculateAsync_LongGpsGapsAreUnknownTimeNotStoppedTime()
    {
        var repository = new InMemoryRepository();
        var tripId = Guid.NewGuid();
        var start = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);
        var track = new List<LocationModel>
        {
            Point(0, 0, 0, 100),
            Point(10, 10, 3, 101),
            Point(110, 10, 0, 100),
            Point(120, 10, 0, 102),
            Point(220, 20, 3, 102),
            Point(230, 30, 3, 102),
        };

        repository.Seed(new Trip
        {
            ID = tripId,
            StartTime = start,
            EndTime = start.AddSeconds(230),
            Locations = track,
        });

        var recalculator = new TripDistanceRecalculator(repository);
        bool changed = false;
        recalculator.Changed += (_, _) => changed = true;
        await recalculator.RecalculateAsync();

        var recalculated = await repository.GetByIdAsync<Trip>(tripId);
        Assert.NotNull(recalculated);
        Assert.Equal(230, recalculated!.TotalTime);
        Assert.Equal(20, recalculated.MovingTime);
        Assert.Equal(10, recalculated.StoppedTime);
        Assert.Equal(recalculated.Distance!.Value / 20, recalculated.AverageSpeed!.Value, 6);
        Assert.Equal(3, recalculated.ElevationGain);
        Assert.True(changed);

        LocationModel Point(int seconds, double latitude, double speed, double altitude) => new()
        {
            ID = Guid.NewGuid(),
            TripID = tripId,
            Latitude = 50 + latitude / 111320,
            Longitude = 8,
            Altitude = altitude,
            Timestamp = start.AddSeconds(seconds),
            Speed = speed,
        };
    }
}

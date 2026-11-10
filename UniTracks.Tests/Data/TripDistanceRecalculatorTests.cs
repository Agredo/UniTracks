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
            Point(0, 0, 0),
            Point(10, 10, 3),
            Point(110, 10, 0),
            Point(120, 10, 0),
            Point(220, 20, 3),
            Point(230, 30, 3),
        };

        repository.Seed(new Trip
        {
            ID = tripId,
            StartTime = start,
            EndTime = start.AddSeconds(230),
            Locations = track,
        });

        await new TripDistanceRecalculator(repository).RecalculateAsync();

        var recalculated = await repository.GetByIdAsync<Trip>(tripId);
        Assert.NotNull(recalculated);
        Assert.Equal(230, recalculated!.TotalTime);
        Assert.Equal(20, recalculated.MovingTime);
        Assert.Equal(10, recalculated.StoppedTime);
        Assert.Equal(recalculated.Distance!.Value / 20, recalculated.AverageSpeed!.Value, 6);

        LocationModel Point(int seconds, double latitude, double speed) => new()
        {
            ID = Guid.NewGuid(),
            TripID = tripId,
            Latitude = 50 + latitude / 111320,
            Longitude = 8,
            Timestamp = start.AddSeconds(seconds),
            Speed = speed,
        };
    }
}

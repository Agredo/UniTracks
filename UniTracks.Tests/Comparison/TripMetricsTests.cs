using UniTracks.Models.Trip;
using UniTracks.Services.Comparison;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.Tests.Comparison;

public class TripMetricsTests
{
    [Fact]
    public void ManualTrip_KeepsStoredDistanceMovementAndAverageSpeed()
    {
        var trip = new Trip
        {
            ID = Guid.NewGuid(),
            StartTime = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero),
            Distance = 5000,
            MovingTime = 1500,
            AverageSpeed = 4,
            Locations = new List<LocationModel>(),
        };

        Assert.Equal(5000, TripMetrics.DistanceMeters(trip, trip.Locations));
        Assert.Equal(1500, TripMetrics.MovingSeconds(trip, trip.Locations));
        Assert.Equal(4, TripMetrics.AverageSpeedMetersPerSecond(trip, trip.Locations));
    }

    [Fact]
    public void RecordedTrack_IgnoresStoredValuesThatDisagreeWithTheGpsPoints()
    {
        var start = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);
        var tripId = Guid.NewGuid();
        var track = new List<LocationModel>
        {
            Point(0, 0, 0),
            Point(10, 10, 3),
            Point(20, 20, 3),
        };
        var trip = new Trip
        {
            ID = Guid.NewGuid(),
            StartTime = start,
            EndTime = start.AddSeconds(20),
            Distance = 5000,
            MovingTime = 1500,
            StoppedTime = 0,
            AverageSpeed = 99,
            Locations = track,
        };

        double distance = TripMetrics.DistanceMeters(trip, track);
        Assert.InRange(distance, 15, 25);
        Assert.Equal(1500, TripMetrics.MovingSeconds(trip, track));
        Assert.Equal(distance / 1500, TripMetrics.AverageSpeedMetersPerSecond(trip, track), 6);

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

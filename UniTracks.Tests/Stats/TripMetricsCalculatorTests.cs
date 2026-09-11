using UniTracks.Services.Stats;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.Tests.Stats;

/// <summary>
/// Moving/stopped time and elevation gain are derived on the fly from the stored GPS points,
/// so every consumer depends on this one implementation staying consistent.
/// </summary>
public sealed class TripMetricsCalculatorTests
{
    [Fact]
    public void ComputeMovingAndStopped_SplitsTimeByTheSpeedOfTheLaterPoint()
    {
        var points = new[]
        {
            Point(0, speed: 0),
            Point(1, speed: 1.0),   // 1 s moving
            Point(2, speed: 1.0),   // 1 s moving
            Point(3, speed: 0.0),   // 1 s stopped
        };

        var (moving, stopped) = TripMetricsCalculator.ComputeMovingAndStopped(points);

        Assert.Equal(TimeSpan.FromSeconds(2), moving);
        Assert.Equal(TimeSpan.FromSeconds(1), stopped);
    }

    [Fact]
    public void ComputeMovingAndStopped_TreatsTheThresholdItselfAsMoving()
    {
        var points = new[]
        {
            Point(0, speed: 0),
            Point(1, speed: TripMetricsCalculator.MovingThresholdMs),
        };

        var (moving, stopped) = TripMetricsCalculator.ComputeMovingAndStopped(points);

        Assert.Equal(TimeSpan.FromSeconds(1), moving);
        Assert.Equal(TimeSpan.Zero, stopped);
    }

    [Fact]
    public void ComputeMovingAndStopped_IgnoresGapsLongerThanTheMaximum()
    {
        var points = new[]
        {
            Point(0, speed: 1.0),
            Point(1, speed: 1.0),                                   // 1 s moving
            Point(1 + (int)TripMetricsCalculator.MaxGap.TotalSeconds + 10, speed: 1.0), // gap → neither
        };

        var (moving, stopped) = TripMetricsCalculator.ComputeMovingAndStopped(points);

        Assert.Equal(TimeSpan.FromSeconds(1), moving);
        Assert.Equal(TimeSpan.Zero, stopped);
    }

    [Fact]
    public void ComputeMovingAndStopped_CountsAGapOfExactlyTheMaximum()
    {
        var points = new[]
        {
            Point(0, speed: 1.0),
            Point((int)TripMetricsCalculator.MaxGap.TotalSeconds, speed: 1.0),
        };

        var (moving, stopped) = TripMetricsCalculator.ComputeMovingAndStopped(points);

        Assert.Equal(TripMetricsCalculator.MaxGap, moving);
        Assert.Equal(TimeSpan.Zero, stopped);
    }

    [Fact]
    public void ComputeMovingAndStopped_IgnoresNonPositiveTimesteps()
    {
        var points = new[]
        {
            Point(5, speed: 1.0),
            Point(5, speed: 1.0), // duplicate timestamp → skipped
            Point(4, speed: 1.0), // out of order → skipped
            Point(5, speed: 1.0), // 1 s moving
        };

        var (moving, stopped) = TripMetricsCalculator.ComputeMovingAndStopped(points);

        Assert.Equal(TimeSpan.FromSeconds(1), moving);
        Assert.Equal(TimeSpan.Zero, stopped);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ComputeMovingAndStopped_WithTooFewPoints_ReturnsZero(int count)
    {
        var points = Enumerable.Range(0, count).Select(i => Point(i, speed: 1.0)).ToArray();

        var (moving, stopped) = TripMetricsCalculator.ComputeMovingAndStopped(points);

        Assert.Equal(TimeSpan.Zero, moving);
        Assert.Equal(TimeSpan.Zero, stopped);
    }

    [Fact]
    public void ComputeElevationGain_SumsOnlyPositiveDeltasAboveTheNoiseFloor()
    {
        var points = new[]
        {
            Point(0, altitude: 0),
            Point(1, altitude: 1.0),    // +1.0 → counts
            Point(2, altitude: 1.2),    // +0.2 → noise
            Point(3, altitude: 0.5),    // −0.7 → descent
            Point(4, altitude: 3.0),    // +2.5 → counts
            Point(5, altitude: 3.4),    // +0.4 → noise
        };

        Assert.Equal(3.5, TripMetricsCalculator.ComputeElevationGain(points), 6);
    }

    [Fact]
    public void ComputeElevationGain_OfADescent_IsZero()
    {
        var points = new[]
        {
            Point(0, altitude: 100),
            Point(1, altitude: 80),
            Point(2, altitude: 60),
        };

        Assert.Equal(0, TripMetricsCalculator.ComputeElevationGain(points));
    }

    [Fact]
    public void ComputeElevationGain_OfASinglePoint_IsZero()
    {
        Assert.Equal(0, TripMetricsCalculator.ComputeElevationGain(new[] { Point(0, altitude: 100) }));
    }

    private static LocationModel Point(
        int second,
        double speed = 0,
        double altitude = 0)
        => new()
        {
            ID = Guid.NewGuid(),
            Latitude = 48.0,
            Longitude = 11.0,
            Altitude = altitude,
            Accuracy = 5,
            Speed = speed,
            Timestamp = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero).AddSeconds(second),
        };
}

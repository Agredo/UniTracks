using UniTracks.Services.Location;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.Tests.Location;

/// <summary>
/// Guards the GPS cleanup pipeline. The interesting cases are the ones that used to produce a
/// wrong route: a teleport that was re-appended after filtering, and endpoints that were dragged
/// inward by the moving average.
/// </summary>
public sealed class TrackSmootherTests
{
    private const double BaseLat = 48.0;
    private const double BaseLon = 11.0;
    private const double MetersPerDegreeLat = 111_320.0;
    private static readonly double MetersPerDegreeLon = MetersPerDegreeLat * Math.Cos(BaseLat * Math.PI / 180);
    private static readonly DateTimeOffset Start = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Smooth_ReturnsACopyForTracksBelowTheMinimumSize()
    {
        var empty = Array.Empty<LocationModel>();
        var single = new[] { Point(0, 0, 0) };
        var pair = new[] { Point(0, 0, 0), Point(1, 10, 0) };

        Assert.Empty(TrackSmoother.Smooth(empty));
        Assert.Single(TrackSmoother.Smooth(single));
        Assert.Equal(2, TrackSmoother.Smooth(pair).Count);

        var result = TrackSmoother.Smooth(single);
        Assert.NotSame(single, result);
        Assert.Same(single[0], result[0]);
    }

    [Fact]
    public void Smooth_DoesNotModifyTheInput()
    {
        var track = StraightTrack(steps: 6);
        var snapshot = track.Select(l => (l.Latitude, l.Longitude, l.Altitude)).ToList();

        TrackSmoother.Smooth(track);

        Assert.Equal(snapshot, track.Select(l => (l.Latitude, l.Longitude, l.Altitude)));
    }

    [Fact]
    public void Smooth_OrdersTheResultByTimestamp()
    {
        var track = StraightTrack(steps: 6);
        var shuffled = track.AsEnumerable().Reverse().ToList();

        var result = TrackSmoother.Smooth(shuffled);

        Assert.Equal(track.Select(l => l.Timestamp), result.Select(l => l.Timestamp));
        Assert.Equal(track[0].ID, result[0].ID);
    }

    [Fact]
    public void Smooth_DropsAccuracyOutliers()
    {
        var track = StraightTrack(steps: 5);
        var outlier = track[2];
        track[2] = outlier with { Accuracy = TrackSmoother.MaxAccuracyMeters + 5 };

        var result = TrackSmoother.Smooth(track);

        Assert.DoesNotContain(result, l => l.ID == outlier.ID);
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void Smooth_KeepsPointsWithUnknownAccuracy()
    {
        var track = StraightTrack(steps: 5);
        foreach (var point in track)
        {
            point.Accuracy = 0; // "unknown" must not be treated as "bad"
        }

        var result = TrackSmoother.Smooth(track);

        Assert.Equal(5, result.Count);
    }

    [Fact]
    public void Smooth_CollapsesStandingJitter_ButKeepsTheLastFix()
    {
        // Barely moving: every fix lies inside the jitter window of the anchor.
        var track = new[]
        {
            Point(0, 0, 0),
            Point(1, 1, 0),
            Point(2, 0.5, 0),
            Point(3, 2, 0),
            Point(4, 1, 0),
        };

        var result = TrackSmoother.Smooth(track);

        // Only the anchor and the re-appended end of the recording survive.
        Assert.Equal(2, result.Count);
        Assert.Equal(track[0].ID, result[0].ID);
        Assert.Equal(track[^1].ID, result[^1].ID);
    }

    [Fact]
    public void Smooth_ReAppendsTheRecordedEndPoint_WhenItFallsInsideTheJitterWindow()
    {
        var track = StraightTrack(steps: 4); // 0, 10, 20, 30 m
        var end = Point(4, 31, 0);          // 1 m past the anchor → dropped as jitter
        var withEnd = track.Append(end).ToList();

        var result = TrackSmoother.Smooth(withEnd);

        // The final fix is re-appended so the route reaches where the recording actually stopped.
        Assert.Equal(withEnd.Count, result.Count);
        Assert.Equal(end.ID, result[^1].ID);
    }

    [Fact]
    public void Smooth_DropsATeleport_AndDoesNotReAppendItAsTheEndPoint()
    {
        var track = StraightTrack(steps: 4);      // 0, 10, 20, 30 m in four seconds
        var teleport = Point(4, 5000, 0);         // 4970 m in one second ≈ 4970 m/s
        var withTeleport = track.Append(teleport).ToList();

        var result = TrackSmoother.Smooth(withTeleport);

        // Filtering drops the teleport; the endpoint rescue must not undo that (it did before:
        // the guard against an implausible jump was missing on the re-append path).
        Assert.Equal(track.Count, result.Count);
        Assert.Equal(track[^1].ID, result[^1].ID);
        Assert.All(result, l => Assert.True(PlanarMeters(track[0], l) < 100, "a teleported fix leaked into the route"));
    }

    [Fact]
    public void Smooth_NeverAveragesTheEndpoints()
    {
        // The last point sits well off the line of the previous ones, so averaging it with its
        // neighbours would visibly pull the rendered route inward.
        var track = new[]
        {
            Point(0, 0, 0),
            Point(1, 10, 0),
            Point(2, 20, 0),
            Point(3, 30, 20),
        };

        var result = TrackSmoother.Smooth(track);

        Assert.Equal(track[0].Latitude, result[0].Latitude);
        Assert.Equal(track[0].Longitude, result[0].Longitude);
        Assert.Equal(track[^1].Latitude, result[^1].Latitude);
        Assert.Equal(track[^1].Longitude, result[^1].Longitude);
    }

    [Fact]
    public void Smooth_AveragesInteriorPoints_AndKeepsTheirMetadata()
    {
        var track = new[]
        {
            Point(0, 0, 0, speed: 1, altitude: 100),
            Point(1, 10, 0, speed: 2, altitude: 101),
            Point(2, 20, 12, speed: 3, altitude: 102),
            Point(3, 30, 0, speed: 4, altitude: 103),
            Point(4, 40, 0, speed: 5, altitude: 104),
        };

        var result = TrackSmoother.Smooth(track);

        Assert.Equal(5, result.Count);

        // Index 2 is averaged over indices 0..4 → its 12 m lateral offset shrinks to 12/5 m.
        Assert.Equal(2.4, EastMeters(result[2]), 6);
        Assert.Equal(20.0, NorthMeters(result[2]), 6);

        // Only the coordinates are smoothed; the timeline metadata stays that of the centre point.
        Assert.Equal(track[2].ID, result[2].ID);
        Assert.Equal(track[2].Timestamp, result[2].Timestamp);
        Assert.Equal(track[2].Speed, result[2].Speed);
        Assert.Equal(track[2].Accuracy, result[2].Accuracy);
    }

    [Fact]
    public void SmoothedDistanceMeters_OfACleanStraightTrack_MatchesTheTrueDistance()
    {
        var track = StraightTrack(steps: 11); // 10 steps of 10 m

        Assert.InRange(TrackSmoother.SmoothedDistanceMeters(track), 99.5, 100.5);
    }

    [Fact]
    public void SmoothedDistanceMeters_OfAStandingTrack_IsNegligible()
    {
        var track = new[]
        {
            Point(0, 0, 0),
            Point(1, 1, 0),
            Point(2, 0.5, 0),
            Point(3, 2, 0),
            Point(4, 1, 0),
        };

        Assert.InRange(TrackSmoother.SmoothedDistanceMeters(track), 0, 2);
    }

    /// <summary>An evenly spaced straight line: <paramref name="steps"/> points, 10 m apart, 1 s apart.</summary>
    private static List<LocationModel> StraightTrack(int steps)
    {
        var track = new List<LocationModel>(steps);
        for (int i = 0; i < steps; i++)
        {
            track.Add(Point(i, i * 10, 0, speed: 1));
        }

        return track;
    }

    private static LocationModel Point(
        int second,
        double northMeters,
        double eastMeters,
        double accuracy = 5,
        double speed = 0,
        double altitude = 0)
        => new()
        {
            ID = Guid.NewGuid(),
            Latitude = BaseLat + northMeters / MetersPerDegreeLat,
            Longitude = BaseLon + eastMeters / MetersPerDegreeLon,
            Altitude = altitude,
            Accuracy = accuracy,
            Speed = speed,
            Timestamp = Start.AddSeconds(second),
        };

    private static double NorthMeters(LocationModel point) => (point.Latitude - BaseLat) * MetersPerDegreeLat;

    private static double EastMeters(LocationModel point) => (point.Longitude - BaseLon) * MetersPerDegreeLon;

    private static double PlanarMeters(LocationModel a, LocationModel b)
    {
        double dy = NorthMeters(b) - NorthMeters(a);
        double dx = EastMeters(b) - EastMeters(a);
        return Math.Sqrt(dx * dx + dy * dy);
    }
}

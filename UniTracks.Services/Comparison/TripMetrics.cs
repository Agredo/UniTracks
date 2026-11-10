using UniTracks.Models.Trip;
using UniTracks.Services.Location;
using UniTracks.Services.Stats;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.Services.Comparison;

/// <summary>One shared interpretation of recorded and stored trip metrics.</summary>
public static class TripMetrics
{
    /// <summary>Total distance of the same raw track that is also used for the kilometre splits.</summary>
    public static double DistanceMeters(Trip trip, IReadOnlyList<LocationModel> ordered)
    {
        var splitDistance = KilometerSplitCalculator.Compute(ordered).Sum(split => split.DistanceMeters);
        // Without a track there is nothing to recalculate (manual trips keep their entered distance).
        // For a recorded track zero is a valid result; never resurrect discarded segments from the
        // old stored distance or from a differently smoothed track.
        return ordered.Count < 2 ? trip.Distance ?? 0 : splitDistance;
    }

    /// <summary>Elapsed wall-clock duration, including pauses and unknown GPS gaps.</summary>
    public static double ElapsedSeconds(Trip trip) =>
        Math.Max(0, trip.TotalTime ?? (trip.EndTime - trip.StartTime).TotalSeconds);

    /// <summary>
    /// A stored zero is a real measurement, not a missing value. Without a track, manual trips
    /// have no evidence of movement and therefore do not fall back to their whole duration.
    /// </summary>
    public static double MovingSeconds(Trip trip, IReadOnlyList<LocationModel> ordered)
    {
        if (trip.MovingTime.HasValue)
        {
            return Math.Max(0, trip.MovingTime.Value);
        }

        return ordered.Count >= 2
            ? TripMetricsCalculator.ComputeMovingAndStopped(ordered).Moving.TotalSeconds
            : 0;
    }

    /// <summary>Distance over moving time, rather than the arithmetic mean of individual GPS speeds.</summary>
    public static double AverageSpeedMetersPerSecond(Trip trip, IReadOnlyList<LocationModel> ordered)
    {
        if (ordered.Count < 2 && trip.AverageSpeed is { } storedSpeed)
        {
            return Math.Max(0, storedSpeed);
        }

        double movingSeconds = MovingSeconds(trip, ordered);
        return movingSeconds > 0 ? DistanceMeters(trip, ordered) / movingSeconds : 0;
    }
}

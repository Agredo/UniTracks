namespace UniTracks.Services.Stats;

/// <summary>
/// Derives per-trip activity metrics (moving/stopped time, elevation gain) from the
/// recorded GPS points. Every consumer shares this implementation so overview, statistics,
/// recording finalisation and comparison stay consistent.
/// </summary>
public static class TripMetricsCalculator
{
    /// <summary>Speed at or above which a point counts as moving (m/s).</summary>
    public const double MovingThresholdMs = 0.5;

    /// <summary>Gaps between fixes longer than this count as neither moving nor stopped.</summary>
    public static readonly TimeSpan MaxGap = TimeSpan.FromSeconds(30);

    /// <summary>Positive altitude deltas smaller than this are treated as GPS noise (m).</summary>
    private const double ElevationNoiseThresholdM = 0.5;

    public static bool IsMoving(Models.Location.Location previous, Models.Location.Location current)
    {
        var elapsed = current.Timestamp - previous.Timestamp;
        // This predicate is shared with split time attribution. Distance thresholds belong to split
        // segmentation, not to sensor-based movement: otherwise the time charged for a split could
        // exceed the time used for the trip itself.
        return elapsed > TimeSpan.Zero
            && elapsed <= MaxGap
            && current.Speed >= MovingThresholdMs;
    }

    /// <param name="ordered">GPS points ordered by timestamp; needs at least two points.</param>
    public static (TimeSpan Moving, TimeSpan Stopped) ComputeMovingAndStopped(IReadOnlyList<Models.Location.Location> ordered)
    {
        double movingSeconds = 0;
        double stoppedSeconds = 0;

        for (int i = 1; i < ordered.Count; i++)
        {
            var dt = ordered[i].Timestamp - ordered[i - 1].Timestamp;
            if (dt <= TimeSpan.Zero || dt > MaxGap)
            {
                continue;
            }

            if (TripMetricsCalculator.IsMoving(ordered[i - 1], ordered[i]))
            {
                movingSeconds += dt.TotalSeconds;
            }
            else
            {
                stoppedSeconds += dt.TotalSeconds;
            }
        }

        return (TimeSpan.FromSeconds(movingSeconds), TimeSpan.FromSeconds(stoppedSeconds));
    }

    /// <summary>Total climbed metres — the sum of positive altitude deltas above the noise floor.</summary>
    public static double ComputeElevationGain(IReadOnlyList<Models.Location.Location> ordered)
    {
        double gain = 0;
        for (int i = 1; i < ordered.Count; i++)
        {
            double delta = ordered[i].Altitude - ordered[i - 1].Altitude;
            if (delta > ElevationNoiseThresholdM)
            {
                gain += delta;
            }
        }

        return gain;
    }
}

namespace UniTracks.Services.Comparison;

/// <summary>
/// Turns a trip's raw numbers into a comparable "how hard was that" measure.
/// <para>
/// A hilly 8 km and a flat 9 km are not the same effort, and neither is 8 km on trails versus 8 km
/// on tarmac. Climbing is converted into an equivalent flat distance so trips from different areas
/// can be lined up on one axis.
/// </para>
/// </summary>
public static class EffortModel
{
    /// <summary>Climb that counts as one extra flat kilometre — the usual route-planning rule of thumb.</summary>
    public const double ClimbMetersPerEquivalentKilometer = 100.0;

    /// <summary>Below this distance the moving-time average is far too noisy to rank trips with.</summary>
    public const double MinimumUsableDistanceMeters = 500.0;

    /// <summary>Distance plus the climbing penalty, in meters.</summary>
    public static double EquivalentDistanceMeters(double distanceMeters, double elevationGainMeters)
    {
        double climb = Math.Max(0, elevationGainMeters);
        return Math.Max(0, distanceMeters) + climb / ClimbMetersPerEquivalentKilometer * 1000.0;
    }

    /// <summary>
    /// Seconds per equivalent kilometre. Falls back to total elapsed time when the moving-time
    /// detection found nothing to latch onto.
    /// </summary>
    public static double EquivalentPaceSecondsPerKilometer(
        double distanceMeters,
        double movingSeconds,
        double elapsedSeconds,
        double elevationGainMeters)
    {
        double equivalentDistance = EquivalentDistanceMeters(distanceMeters, elevationGainMeters);

        if (equivalentDistance < MinimumUsableDistanceMeters)
        {
            return 0;
        }

        double seconds = movingSeconds > 0 ? movingSeconds : elapsedSeconds;

        return seconds <= 0 ? 0 : seconds / (equivalentDistance / 1000.0);
    }

    /// <summary>
    /// A single scalar that stands in for "how much work did this trip take": meters of equivalent
    /// distance over moving time, expressed as an average speed on flat ground.
    /// <para>
    /// Kept dimensionless on purpose — it exists to bucket and rank trips, never to be shown.
    /// </para>
    /// </summary>
    public static double EffortScore(
        double distanceMeters,
        double movingSeconds,
        double elapsedSeconds,
        double elevationGainMeters)
    {
        double pace = EquivalentPaceSecondsPerKilometer(distanceMeters, movingSeconds, elapsedSeconds, elevationGainMeters);
        return pace <= 0 ? 0 : 1000.0 / pace;
    }

    /// <summary>
    /// How similar two effort scores are, as 0..1. A 10 % difference in equivalent speed still
    /// counts as "comparable", which is the band a runner would call their own normal day-to-day
    /// variation.
    /// </summary>
    public static double EffortSimilarity(double effortScoreA, double effortScoreB)
    {
        if (effortScoreA <= 0 || effortScoreB <= 0)
        {
            return 0;
        }

        double ratio = Math.Min(effortScoreA, effortScoreB) / Math.Max(effortScoreA, effortScoreB);
        return Math.Clamp(ratio, 0, 1);
    }

    /// <summary>
    /// How similar two trip lengths are, as 0..1, compared on equivalent distance so climbing is
    /// accounted for.
    /// </summary>
    public static double DistanceSimilarity(double equivalentDistanceMetersA, double equivalentDistanceMetersB)
    {
        if (equivalentDistanceMetersA <= 0 || equivalentDistanceMetersB <= 0)
        {
            return 0;
        }

        return Math.Clamp(
            Math.Min(equivalentDistanceMetersA, equivalentDistanceMetersB) / Math.Max(equivalentDistanceMetersA, equivalentDistanceMetersB),
            0,
            1);
    }
}

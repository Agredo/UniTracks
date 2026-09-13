using UniTracks.Services.Comparison;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.Services.Stats;

/// <summary>One kilometre of a trip, or the shorter remainder at the very end.</summary>
public sealed record KilometerSplit
{
    /// <summary>1-based split number, so split 3 covers the third kilometre.</summary>
    public required int Number { get; init; }

    public required double DistanceMeters { get; init; }

    /// <summary>Time spent actually moving inside the split; standing still is not charged to pace.</summary>
    public required TimeSpan MovingTime { get; init; }

    /// <summary>Elapsed time from entering the split to leaving it, including any standstill.</summary>
    public required TimeSpan ElapsedTime { get; init; }

    public required double ElevationGainMeters { get; init; }

    public required double NetElevationMeters { get; init; }

    /// <summary>True for the trailing partial kilometre, which cannot be compared with a full one.</summary>
    public bool IsPartial { get; init; }

    public double AverageSpeedMetersPerSecond =>
        MovingTime.TotalSeconds <= 0 ? 0 : DistanceMeters / MovingTime.TotalSeconds;

    /// <summary>Seconds per kilometre over the split's moving time; 0 when the split is unusable.</summary>
    public double PaceSecondsPerKilometer =>
        DistanceMeters <= 0 ? 0 : MovingTime.TotalSeconds / (DistanceMeters / 1000.0);

    /// <summary>Pace with the climbing converted into extra distance, so a hilly kilometre is not called slow.</summary>
    public double EquivalentPaceSecondsPerKilometer =>
        MovingTime.TotalSeconds <= 0
            ? 0
            : MovingTime.TotalSeconds / (EffortModel.EquivalentDistanceMeters(DistanceMeters, ElevationGainMeters) / 1000.0);
}

/// <summary>
/// Splits a track into kilometres.
/// <para>
/// Derived on demand rather than stored: splits are only ever needed for the one or two trips on
/// screen, and keeping them out of the database means the definition can change without leaving
/// stale rows behind.
/// </para>
/// </summary>
public static class KilometerSplitCalculator
{
    /// <summary>Kilometre length every split but the last aims for.</summary>
    public const double SplitLengthMeters = 1000.0;

    /// <summary>Fix pairs closer together than this are GPS noise and contribute nothing.</summary>
    public const double MinimumSegmentDistanceMeters = 1.0;

    /// <summary>Matches the trip statistics' walking/stopped cut-off so both agree on moving time.</summary>
    private const double MovingThresholdMetersPerSecond = TripMetricsCalculator.MovingThresholdMs;

    public static IReadOnlyList<KilometerSplit> Compute(IReadOnlyList<LocationModel> track)
    {
        if (track is null || track.Count < 2)
        {
            return Array.Empty<KilometerSplit>();
        }

        var splits = new List<KilometerSplit>();

        int number = 1;
        double splitDistance = 0;
        double splitMoving = 0;
        double splitElapsed = 0;
        double splitGain = 0;
        double splitStartAltitude = track[0].Altitude;
        double splitEndAltitude = splitStartAltitude;

        for (int i = 1; i < track.Count; i++)
        {
            var previous = track[i - 1];
            var current = track[i];

            double segmentDistance = GeoMath.HaversineMeters(previous.Latitude, previous.Longitude, current.Latitude, current.Longitude);
            var gap = current.Timestamp - previous.Timestamp;
            double segmentSeconds = gap.TotalSeconds;

            // Skip unusable pairs wholesale: a step shorter than a single GPS wobble, a zero-length
            // step, or a timestamp that does not advance all say nothing about pace.
            if (segmentDistance < MinimumSegmentDistanceMeters || segmentSeconds <= 0)
            {
                continue;
            }

            bool isMoving = gap <= TripMetricsCalculator.MaxGap
                && segmentDistance / segmentSeconds >= MovingThresholdMetersPerSecond;

            double segmentMoving = isMoving ? segmentSeconds : 0;
            double altitudeChange = current.Altitude - previous.Altitude;
            double segmentGain = Math.Max(0, altitudeChange);

            double consumed = 0;

            while (consumed < segmentDistance)
            {
                double take = Math.Min(SplitLengthMeters - splitDistance, segmentDistance - consumed);
                double fraction = take / segmentDistance;

                splitDistance += take;
                splitElapsed += segmentSeconds * fraction;
                splitMoving += segmentMoving * fraction;
                splitGain += segmentGain * fraction;
                consumed += take;

                splitEndAltitude = previous.Altitude + altitudeChange * (consumed / segmentDistance);

                if (splitDistance < SplitLengthMeters)
                {
                    continue;
                }

                splits.Add(new KilometerSplit
                {
                    Number = number,
                    DistanceMeters = SplitLengthMeters,
                    MovingTime = TimeSpan.FromSeconds(splitMoving),
                    ElapsedTime = TimeSpan.FromSeconds(splitElapsed),
                    ElevationGainMeters = splitGain,
                    NetElevationMeters = splitEndAltitude - splitStartAltitude,
                });

                number++;
                splitDistance = 0;
                splitMoving = 0;
                splitElapsed = 0;
                splitGain = 0;
                splitStartAltitude = splitEndAltitude;
            }
        }

        if (splitDistance >= MinimumSegmentDistanceMeters)
        {
            splits.Add(new KilometerSplit
            {
                Number = number,
                DistanceMeters = splitDistance,
                MovingTime = TimeSpan.FromSeconds(splitMoving),
                ElapsedTime = TimeSpan.FromSeconds(splitElapsed),
                ElevationGainMeters = splitGain,
                NetElevationMeters = splitEndAltitude - splitStartAltitude,
                IsPartial = true,
            });
        }

        return splits;
    }
}

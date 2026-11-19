using UniTracks.Services.Comparison;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.Services.Stats;

/// <summary>Fastest time over a fixed distance inside one trip, e.g. the best 5 km of a 12 km run.</summary>
public sealed record BestEffort
{
    public required double DistanceMeters { get; init; }

    /// <summary>Moving seconds for the whole distance; standing still is not charged.</summary>
    public required double Seconds { get; init; }

    public double PaceSecondsPerKilometer =>
        DistanceMeters <= 0 || Seconds <= 0 ? 0 : Seconds / (DistanceMeters / 1000.0);
}

/// <summary>
/// Finds the fastest stretch of a given length inside a track: the "best 1 km", "best 5 km" and
/// "best 10 km" of a trip. Uses the same segment rules as <see cref="KilometerSplitCalculator"/>,
/// so a best effort never spans a GPS gap and never charges stopped time.
/// </summary>
public static class BestEffortCalculator
{
    /// <summary>The distances best efforts are usually quoted for, in meters.</summary>
    public static readonly IReadOnlyList<double> DefaultDistances = new[] { 1000.0, 5000.0, 10000.0 };

    /// <summary>Fix pairs closer together than this are GPS noise and contribute nothing.</summary>
    private const double MinimumSegmentDistanceMeters = 1.0;

    /// <summary>Absorbs floating-point drift when a run covers exactly the requested distance.</summary>
    private const double DistanceEpsilonMeters = 0.01;

    /// <summary>
    /// Fastest moving time for every requested distance the track actually covers, ascending.
    /// Distances longer than the trip are omitted rather than reported as the whole trip.
    /// </summary>
    public static IReadOnlyList<BestEffort> Compute(
        IReadOnlyList<LocationModel> track,
        IReadOnlyList<double>? distances = null)
    {
        if (track is null || track.Count < 2)
        {
            return Array.Empty<BestEffort>();
        }

        var runs = BuildRuns(track);
        if (runs.Count == 0)
        {
            return Array.Empty<BestEffort>();
        }

        var results = new List<BestEffort>();
        foreach (double target in (distances ?? DefaultDistances).Where(value => value > 0).Order())
        {
            double best = FastestWindowSeconds(runs, target);
            if (best > 0)
            {
                results.Add(new BestEffort { DistanceMeters = target, Seconds = best });
            }
        }

        return results;
    }

    /// <summary>
    /// One unbroken chain of usable GPS segments. A GPS gap or a stationery fix starts a new chain,
    /// because a best effort must never average across data it does not have.
    /// </summary>
    private static List<List<TrackSegment>> BuildRuns(IReadOnlyList<LocationModel> ordered)
    {
        var runs = new List<List<TrackSegment>>();
        List<TrackSegment>? current = null;

        for (int index = 1; index < ordered.Count; index++)
        {
            var previous = ordered[index - 1];
            var next = ordered[index];
            var gap = next.Timestamp - previous.Timestamp;

            double distance = GeoMath.HaversineMeters(
                previous.Latitude,
                previous.Longitude,
                next.Latitude,
                next.Longitude);

            bool usable = distance >= MinimumSegmentDistanceMeters && gap > TimeSpan.Zero;
            if (!usable)
            {
                // Stationary fixes and GPS wobble say nothing about pace; they do not end the chain.
                continue;
            }

            // Time and distance lost to a long GPS gap are unknown, so the chain restarts there.
            if (gap > TripMetricsCalculator.MaxGap)
            {
                current = null;
                continue;
            }

            if (current is null)
            {
                current = new List<TrackSegment>();
                runs.Add(current);
            }

            current.Add(new TrackSegment(
                distance,
                TripMetricsCalculator.IsMoving(previous, next) ? gap.TotalSeconds : 0));
        }

        return runs;
    }

    /// <summary>Slides a window of the requested length over every run and keeps the fastest one.</summary>
    private static double FastestWindowSeconds(List<List<TrackSegment>> runs, double target)
    {
        double best = 0;

        foreach (var run in runs)
        {
            if (run.Sum(segment => segment.DistanceMeters) + DistanceEpsilonMeters < target)
            {
                continue;
            }

            int end = 0;
            double windowDistance = 0;
            double windowSeconds = 0;

            for (int start = 0; start < run.Count; start++)
            {
                while (end < run.Count && windowDistance + DistanceEpsilonMeters < target)
                {
                    windowDistance += run[end].DistanceMeters;
                    windowSeconds += run[end].Seconds;
                    end++;
                }

                if (windowDistance + DistanceEpsilonMeters < target)
                {
                    break;
                }

                double seconds = windowSeconds;
                double overshoot = windowDistance - target;
                if (overshoot > 0)
                {
                    var last = run[end - 1];
                    double fraction = Math.Clamp(overshoot / last.DistanceMeters, 0, 1);
                    seconds -= last.Seconds * fraction;
                }

                if (seconds > 0 && (best <= 0 || seconds < best))
                {
                    best = seconds;
                }

                windowDistance -= run[start].DistanceMeters;
                windowSeconds -= run[start].Seconds;
            }
        }

        return best;
    }

    private readonly record struct TrackSegment(double DistanceMeters, double Seconds);
}

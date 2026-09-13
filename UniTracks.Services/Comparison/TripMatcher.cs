using UniTracks.Models.Comparison;

namespace UniTracks.Services.Comparison;

public enum TripMatchKind
{
    /// <summary>The other trip covered the same ground.</summary>
    SameRoute,

    /// <summary>The other trip was somewhere else but asked for a comparable performance.</summary>
    ComparableEffort,
}

/// <summary>One candidate trip together with why it was suggested.</summary>
public sealed record TripMatch
{
    public required TripFingerprint Candidate { get; init; }

    public required TripMatchKind Kind { get; init; }

    /// <summary>0..1; only comparable within one <see cref="Kind"/>.</summary>
    public required double Score { get; init; }

    /// <summary>Short German verdict shown on the card, e.g. "Gleiche Strecke".</summary>
    public required string Label { get; init; }

    /// <summary>Largest distance between the two routes, in meters. Same-route matches only.</summary>
    public double ShapeDistanceMeters { get; init; }

    /// <summary>Share of route cells the two trips have in common. Same-route matches only.</summary>
    public double CellOverlap { get; init; }

    /// <summary>True when the other trip covered the route the other way round.</summary>
    public bool IsReversed { get; init; }

    /// <summary>Equivalent-speed difference in m/s, candidate minus target. Comparable-effort matches only.</summary>
    public double EffortDeltaMetersPerSecond { get; init; }
}

/// <summary>
/// Decides which trips belong together. Pure and synchronous: the caller supplies fingerprints, this
/// only does the geometry and the ranking, which keeps every rule testable without a database.
/// </summary>
public static class TripMatcher
{
    /// <summary>
    /// Score from which two trips count as the same route rather than a similar one. Below it the
    /// match is still worth showing, but as "ähnliche Strecke" - the caller marks those rows so an
    /// athlete can tell a repeat from a lookalike.
    /// </summary>
    public const double IdenticalRouteScore = 0.85;

    /// <summary>Score from which a similar route is called a very similar one.</summary>
    public const double SimilarRouteScore = 0.60;

    /// <summary>
    /// Trips that ran the same route as <paramref name="target"/>, best first and excluding the target.
    /// <para>
    /// The cell overlap is the cheap pre-filter (same ground?), the Fréchet distance is the honest
    /// test (same line, same direction?), and the length check keeps an out-and-back from matching a
    /// one-way leg over the same ground. The trip type is a hard gate: a route history that mixes a
    /// run with a dog walk has no meaningful personal best.
    /// </para>
    /// </summary>
    public static IReadOnlyList<TripMatch> FindSameRoute(
        TripFingerprint target,
        IEnumerable<TripFingerprint> candidates,
        TripSimilarityOptions? options = null)
    {
        var effective = options ?? TripSimilarityOptions.Default;
        var targetCells = new HashSet<string>(target.RouteCells);
        var targetPolyline = TripFingerprintBuilder.Polyline(target);

        if (targetCells.Count == 0 || targetPolyline.Count < 2)
        {
            return Array.Empty<TripMatch>();
        }

        var matches = new List<TripMatch>();

        foreach (var candidate in candidates)
        {
            if (candidate.TripID == target.TripID)
            {
                continue;
            }

            if (!SharesSportFamily(target, candidate, effective))
            {
                continue;
            }

            if (effective.RequireSameTripType && !SharesTripType(target, candidate, effective))
            {
                continue;
            }

            double overlap = CellOverlap(targetCells, candidate.RouteCells);
            if (overlap < effective.RouteMinimumCellOverlap)
            {
                continue;
            }

            double lengthRatio = RelativeDifference(target.DistanceMeters, candidate.DistanceMeters);
            if (lengthRatio > effective.RouteLengthTolerance)
            {
                continue;
            }

            var candidatePolyline = TripFingerprintBuilder.Polyline(candidate);
            if (candidatePolyline.Count < 2)
            {
                continue;
            }

            // Comparing against the reversed line as well separates "same route, other direction"
            // from "actually a different shape", which a plain overlap count cannot do.
            double forward = GeoMath.FrechetMeters(targetPolyline, candidatePolyline);
            double reversed = GeoMath.FrechetMeters(targetPolyline, Reverse(candidatePolyline));
            bool isReversed = reversed < forward;

            double shapeDistance = Math.Min(forward, reversed);
            if (shapeDistance > effective.RouteMaximumShapeDistanceMeters)
            {
                continue;
            }

            double overlapScore = BandScore(overlap, effective.RouteStrongCellOverlap, effective.RouteMinimumCellOverlap);
            double shapeScore = BandScore(shapeDistance, effective.RouteStrongShapeDistanceMeters, effective.RouteMaximumShapeDistanceMeters);
            double lengthScore = 1 - lengthRatio / effective.RouteLengthTolerance;

            double score = 0.5 * overlapScore + 0.4 * shapeScore + 0.1 * Math.Clamp(lengthScore, 0, 1);

            matches.Add(new TripMatch
            {
                Candidate = candidate,
                Kind = TripMatchKind.SameRoute,
                Score = score,
                Label = RouteLabel(score, isReversed),
                ShapeDistanceMeters = shapeDistance,
                CellOverlap = overlap,
                IsReversed = isReversed,
            });
        }

        Trim(matches, effective.MaxSameRouteResults);
        return matches;
    }

    /// <summary>
    /// Trips somewhere else that demanded a comparable performance — the same kind of day, not the
    /// same ground. Called with <paramref name="excluded"/> holding the same-route results so a route
    /// never shows up in both sections.
    /// <para>
    /// Only the sport family is a hard gate here: a swim is never a run's comparable effort. The fine
    /// trip type follows the same switch as the route search, because a fast walk and a slow run can
    /// land on the same effort score by accident and that is not a comparison anyone asked for.
    /// </para>
    /// </summary>
    public static IReadOnlyList<TripMatch> FindComparableEffort(
        TripFingerprint target,
        IEnumerable<TripFingerprint> candidates,
        IReadOnlyCollection<Guid>? excluded = null,
        TripSimilarityOptions? options = null)
    {
        var effective = options ?? TripSimilarityOptions.Default;

        if (target.EffortScore <= 0 || target.EquivalentDistanceMeters < effective.EffortMinimumDistanceMeters)
        {
            return Array.Empty<TripMatch>();
        }

        var matches = new List<TripMatch>();

        foreach (var candidate in candidates)
        {
            if (candidate.TripID == target.TripID || excluded?.Contains(candidate.TripID) == true)
            {
                continue;
            }

            if (candidate.EffortScore <= 0 || candidate.EquivalentDistanceMeters < effective.EffortMinimumDistanceMeters)
            {
                continue;
            }

            if (!SharesSportFamily(target, candidate, effective))
            {
                continue;
            }

            if (effective.RequireSameTripType && !SharesTripType(target, candidate, effective))
            {
                continue;
            }

            double distanceSimilarity = EffortModel.DistanceSimilarity(target.EquivalentDistanceMeters, candidate.EquivalentDistanceMeters);
            if (distanceSimilarity < effective.EffortMinimumDistanceSimilarity)
            {
                continue;
            }

            double effortSimilarity = EffortModel.EffortSimilarity(target.EffortScore, candidate.EffortScore);
            if (effortSimilarity < effective.EffortMinimumScoreSimilarity)
            {
                continue;
            }

            // Climbing character: figures in so a flat 10 km is not sold as the twin of a 10 km
            // with 400 m of ascent that happens to average out the same.
            double climbSimilarity = RelativeSimilarity(
                ClimbDensity(target),
                ClimbDensity(candidate));

            bool sameTripType = SharesTripType(target, candidate, effective);

            double score = 0.35 * distanceSimilarity
                + 0.40 * effortSimilarity
                + 0.10 * climbSimilarity
                + effective.EffortTripTypeWeight * (sameTripType ? 1 : 0);

            double effortDelta = candidate.EffortScore - target.EffortScore;

            matches.Add(new TripMatch
            {
                Candidate = candidate,
                Kind = TripMatchKind.ComparableEffort,
                Score = score,
                Label = Label(effortSimilarity, effective, sameTripType, candidate.TripIdentifier),
                EffortDeltaMetersPerSecond = effortDelta,
            });
        }

        Trim(matches, effective.MaxComparableEffortResults);
        return matches;
    }

    /// <summary>Dice coefficient of two cell sets: 1 means identical ground, 0 means nothing shared.</summary>
    public static double CellOverlap(HashSet<string> cellsA, IReadOnlyCollection<string> cellsB)
    {
        if (cellsA.Count == 0 || cellsB.Count == 0)
        {
            return 0;
        }

        int shared = 0;
        var seen = new HashSet<string>();
        foreach (string cell in cellsB)
        {
            if (seen.Add(cell) && cellsA.Contains(cell))
            {
                shared++;
            }
        }

        // Dice divides by the two set sizes, not by the union: a short trip whose cells are all
        // contained in a long one used to score 2 * |A| / |B| - above 1 - and was then labelled a
        // match on a route it only touches at the start.
        int total = cellsA.Count + seen.Count;
        return total == 0 ? 0 : 2.0 * shared / total;
    }

    /// <summary>
    /// Whether two trips belong to the same coarse sport family ("running", "cycling", …). This is the
    /// gate that keeps a swim out of a run's comparison, and it is deliberately the <em>only</em> gate
    /// that applies everywhere.
    /// </summary>
    private static bool SharesSportFamily(TripFingerprint a, TripFingerprint b, TripSimilarityOptions options)
    {
        // A trip without a type says nothing about its sport family. Comparing it against a typed trip
        // could pair a run with a swim, so by default unknown only meets unknown.
        bool aKnown = !string.IsNullOrEmpty(a.TripCategory);
        bool bKnown = !string.IsNullOrEmpty(b.TripCategory);

        if (!aKnown || !bKnown)
        {
            return options.UnknownTripTypeMatchesAnything || aKnown == bKnown;
        }

        return string.Equals(a.TripCategory, b.TripCategory, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Whether two trips are the same kind of session. Walk and Run share the category "running", and
    /// they are different training, so this is the stricter check.
    /// </summary>
    private static bool SharesTripType(TripFingerprint a, TripFingerprint b, TripSimilarityOptions options)
    {
        bool aKnown = !string.IsNullOrEmpty(a.TripIdentifier);
        bool bKnown = !string.IsNullOrEmpty(b.TripIdentifier);

        if (!aKnown || !bKnown)
        {
            return options.UnknownTripTypeMatchesAnything || aKnown == bKnown;
        }

        return string.Equals(a.TripIdentifier, b.TripIdentifier, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Wording for a comparable-effort hit, so the list explains what it is rather than scoring it.</summary>
    private static string Label(
        double effortSimilarity,
        TripSimilarityOptions options,
        bool sameTripType,
        string candidateIdentifier)
    {
        string basis = effortSimilarity >= options.EffortStrongScoreSimilarity
            ? "Vergleichbare Leistung"
            : "Ähnlicher Umfang";

        // Same family, different discipline (a trail run against a flat run). Worth naming, because it
        // is a fair comparison but not a like-for-like one.
        return sameTripType || string.IsNullOrEmpty(candidateIdentifier)
            ? basis
            : $"{basis} · {candidateIdentifier}";
    }

    private static double ClimbDensity(TripFingerprint fingerprint) =>
        fingerprint.DistanceMeters <= 0 ? 0 : fingerprint.ElevationGainMeters / fingerprint.DistanceMeters;

    private static double RelativeSimilarity(double a, double b)
    {
        double maximum = Math.Max(Math.Abs(a), Math.Abs(b));
        return maximum <= 0 ? 1 : Math.Clamp(1 - Math.Abs(a - b) / maximum, 0, 1);
    }

    private static double RelativeDifference(double a, double b)
    {
        double maximum = Math.Max(a, b);
        return maximum <= 0 ? 1 : Math.Abs(a - b) / maximum;
    }

    /// <summary>
    /// Maps a measurement onto 0..1 where <paramref name="strongValue"/> and better scores 1 and
    /// <paramref name="weakValue"/> scores 0. Written so either direction works: for the overlap the
    /// strong value is above the weak one, for the shape distance it is below.
    /// </summary>
    private static double BandScore(double value, double strongValue, double weakValue)
    {
        if (strongValue == weakValue)
        {
            return value == strongValue ? 1 : 0;
        }

        double position = (value - weakValue) / (strongValue - weakValue);
        return Math.Clamp(position, 0, 1);
    }

    private static string RouteLabel(double score, bool isReversed)
    {
        string label = score >= IdenticalRouteScore
            ? "Gleiche Strecke"
            : score >= SimilarRouteScore
                ? "Sehr ähnliche Strecke"
                : "Ähnliche Strecke";

        return isReversed ? label + ", andersrum" : label;
    }

    private static void Trim(List<TripMatch> matches, int maximum)
    {
        matches.Sort((a, b) => b.Score.CompareTo(a.Score));

        if (matches.Count > maximum)
        {
            matches.RemoveRange(maximum, matches.Count - maximum);
        }
    }

    private static IReadOnlyList<GeoPoint> Reverse(IReadOnlyList<GeoPoint> path)
    {
        var reversed = new GeoPoint[path.Count];
        for (int i = 0; i < path.Count; i++)
        {
            reversed[i] = path[path.Count - 1 - i];
        }

        return reversed;
    }
}

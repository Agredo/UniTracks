namespace UniTracks.Services.Comparison;

/// <summary>
/// The tunable knobs of the similarity search. Grouped in one place because every threshold here is
/// a judgement call that has to be calibrated against real recordings, not a constant of nature.
/// </summary>
public sealed record TripSimilarityOptions
{
    public static TripSimilarityOptions Default { get; } = new();

    // ---- Same route (shape) ----

    /// <summary>Cell overlap below this is never the same route, however good the shape looks.</summary>
    public double RouteMinimumCellOverlap { get; init; } = 0.25;

    /// <summary>Cell overlap from here on counts as full marks; GPS drift makes a perfect match unrealistic.</summary>
    public double RouteStrongCellOverlap { get; init; } = 0.60;

    /// <summary>Shape mismatch a same-route pair is allowed to have at its worst point.</summary>
    public double RouteMaximumShapeDistanceMeters { get; init; } = 250;

    /// <summary>Shape mismatch that still counts as full marks.</summary>
    public double RouteStrongShapeDistanceMeters { get; init; } = 80;

    /// <summary>Relative length difference a same-route pair may have (0.35 = 35 %).</summary>
    public double RouteLengthTolerance { get; init; } = 0.35;

    /// <summary>Ranked same-route results returned.</summary>
    public int MaxSameRouteResults { get; init; } = 12;

    // ---- Comparable effort (elsewhere) ----

    /// <summary>Equivalent-length similarity two comparable trips need (0.75 = within 25 %).</summary>
    public double EffortMinimumDistanceSimilarity { get; init; } = 0.75;

    /// <summary>Equivalent-speed similarity two comparable trips need (0.80 = within 20 %).</summary>
    public double EffortMinimumScoreSimilarity { get; init; } = 0.80;

    /// <summary>Above this the trips are as close as day-to-day variation gets.</summary>
    public double EffortStrongScoreSimilarity { get; init; } = 0.93;

    /// <summary>A trip below this length has too noisy an average to compare demands with.</summary>
    public double EffortMinimumDistanceMeters { get; init; } = 1000;

    /// <summary>Ranked comparable-effort results returned.</summary>
    public int MaxComparableEffortResults { get; init; } = 12;

    // ---- Trip type ----

    /// <summary>
    /// Whether a match must be the same fine trip type. On by default, which is the safe reading:
    /// Walk and Run share the category "running", so a trip type of "run" must not quietly collect dog
    /// walks on the same route. Turning this off is the user-facing "alle Lauftypen einbeziehen"
    /// filter — comparisons stay inside the sport family either way.
    /// </summary>
    public bool RequireSameTripType { get; init; } = true;

    /// <summary>
    /// Whether trips without a type may match typed trips. Off by default — a comparison across sport
    /// families is exactly the wrong answer, and "no type" says nothing about the family. The
    /// "alle Lauftypen einbeziehen" toggle turns it on: a trip recorded without a Lauftyp is exactly
    /// the case the switch is for, and leaving it strict made those trips look uncomparable.
    /// </summary>
    public bool UnknownTripTypeMatchesAnything { get; init; }

    /// <summary>How much an identical trip type adds to a comparable-effort score.</summary>
    public double EffortTripTypeWeight { get; init; } = 0.15;

    /// <summary>
    /// The same options with the fine trip type filter switched on or off. Used by the
    /// "alle Lauftypen einbeziehen" toggle so the whole feature relaxes in one place.
    /// </summary>
    public TripSimilarityOptions WithTripTypeFilter(bool includeAllTripTypes) => this with
    {
        RequireSameTripType = !includeAllTripTypes,
        UnknownTripTypeMatchesAnything = includeAllTripTypes,
    };
}

namespace UniTracks.Services.Comparison;

/// <summary>
/// One trip inside a multi-trip comparison. The member at index 0 is the basis: every difference on
/// the comparison page is measured against it.
/// </summary>
public sealed record TripComparisonMember
{
    public required int Index { get; init; }

    public required TripComparisonSide Side { get; init; }

    /// <summary>Colour that stands for this trip on the map, in the chart and in the splits.</summary>
    public required string ColorHex { get; init; }

    public bool IsBaseline => Index == 0;

    public string Name => Side.Name;

    /// <summary>Equivalent pace in seconds per kilometre; 0 when the trip has no usable data.</summary>
    public double EquivalentPaceSecondsPerKilometer => Side.EquivalentPaceSecondsPerKilometer;
}

/// <summary>One kilometre with the pace of every member, so the km can be compared across all runs.</summary>
public sealed record SplitMatrixRow
{
    public required int Number { get; init; }

    /// <summary>Equivalent pace per member, in seconds. 0 when that member did not run this kilometre.</summary>
    public required IReadOnlyList<double> SecondsPerMember { get; init; }

    /// <summary>Difference against the basis per member, in seconds. 0 for the basis itself.</summary>
    public required IReadOnlyList<double> DeltaSecondsPerMember { get; init; }

    /// <summary>True when any member's kilometre is a shorter remainder.</summary>
    public required bool IsPartial { get; init; }

    /// <summary>Member with the fastest pace on this kilometre, or -1 when nobody has data for it.</summary>
    public required int FastestMemberIndex { get; init; }
}

/// <summary>One line of the ranking: the member's place on the climb-adjusted pace.</summary>
public sealed record TripComparisonRank
{
    /// <summary>1 for the fastest run of the comparison.</summary>
    public required int Rank { get; init; }

    public required TripComparisonMember Member { get; init; }

    /// <summary>Climb-adjusted pace against the basis, in seconds per kilometre. Negative is faster.</summary>
    public required double DeltaSecondsPerKilometer { get; init; }

    public bool IsFastest => Rank == 1;

    public bool IsBaseline => Member.IsBaseline;
}

/// <summary>
/// The comparison of two or more trips against a basis: the members, the kilometre matrix, the pace
/// curve per member, the ranking and the headline verdict. Built by
/// <see cref="TripComparisonCalculator.CompareSet"/>.
/// </summary>
public sealed record TripComparisonSet
{
    /// <summary>All compared trips, basis first. Never fewer than two.</summary>
    public required IReadOnlyList<TripComparisonMember> Members { get; init; }

    /// <summary>Kilometres all members covered, so the curves and the columns line up.</summary>
    public required IReadOnlyList<SplitMatrixRow> Splits { get; init; }

    /// <summary>Axis captions for the pace curves, e.g. "1", "2", "3".</summary>
    public required IReadOnlyList<string> DistanceAxisLabels { get; init; }

    /// <summary>Pace curve per member, in the same order as <see cref="Members"/>.</summary>
    public required IReadOnlyList<IReadOnlyList<double>> PaceSeries { get; init; }

    /// <summary>Ranking by climb-adjusted pace, fastest first.</summary>
    public required IReadOnlyList<TripComparisonRank> Ranking { get; init; }

    public required string Verdict { get; init; }

    public required string VerdictDetail { get; init; }

    /// <summary>Set when the trips did not ask for quite the same thing, e.g. more climbing.</summary>
    public string? FairnessNote { get; init; }

    public TripComparisonMember Baseline => Members[0];

    public int Count => Members.Count;

    public bool HasSplits => Splits.Count > 0;

    /// <summary>True for the classic two-trip comparison, which keeps the wording it always had.</summary>
    public bool IsPair => Members.Count == 2;

    /// <summary>Difference between the fastest and the slowest run, in seconds per kilometre.</summary>
    public double PaceSpreadSecondsPerKilometer
    {
        get
        {
            var measurable = Members
                .Select(member => member.EquivalentPaceSecondsPerKilometer)
                .Where(pace => pace > 0)
                .ToList();

            return measurable.Count < 2 ? 0 : measurable.Max() - measurable.Min();
        }
    }
}

/// <summary>
/// The colours the comparison uses. Shared by the service (which assigns them) and the page, so a
/// trip keeps its colour from the legend to the splits.
/// </summary>
public static class TripComparisonPalette
{
    /// <summary>Eight distinguishable colours on the dark theme; the basis always takes the first.</summary>
    public static readonly IReadOnlyList<string> Colors = new[]
    {
        "#4D9FE7",
        "#FF6B3D",
        "#4DE790",
        "#B06BE7",
        "#E7C34D",
        "#E74D8C",
        "#4DE7DB",
        "#C4E74D",
    };

    /// <summary>Most trips one comparison can show: beyond this the curves stop being tellable apart.</summary>
    public const int MaxMembers = 8;

    public static string ColorFor(int index) => Colors[Math.Abs(index) % Colors.Count];
}

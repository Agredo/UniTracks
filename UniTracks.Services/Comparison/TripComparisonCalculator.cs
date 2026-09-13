using UniTracks.Models.Trip;
using UniTracks.Services.Stats;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.Services.Comparison;

/// <summary>One side of a comparison: the metrics, the track for the map and the kilometre splits.</summary>
public sealed record TripComparisonSide
{
    public required Trip Trip { get; init; }

    /// <summary>Smoothed track, ready to be drawn. Only ever held for the two trips on screen.</summary>
    public required IReadOnlyList<LocationModel> Track { get; init; }

    public required IReadOnlyList<KilometerSplit> Splits { get; init; }

    public required double DistanceMeters { get; init; }

    public required double MovingSeconds { get; init; }

    public required double ElapsedSeconds { get; init; }

    public required double ElevationGainMeters { get; init; }

    public required double MaxSpeedMetersPerSecond { get; init; }

    /// <summary>Climb-adjusted pace, in seconds per kilometre. How the two sides are compared.</summary>
    public required double EquivalentPaceSecondsPerKilometer { get; init; }

    public string Name => TripDisplay.Name(Trip);

    public double DistanceKilometers => DistanceMeters / 1000.0;

    public double AverageSpeedMetersPerSecond =>
        MovingSeconds <= 0 ? 0 : DistanceMeters / MovingSeconds;
}

/// <summary>Difference between the two sides on one kilometre.</summary>
public sealed record SplitDelta
{
    public required int Number { get; init; }

    /// <summary>Equivalent pace of the reference trip on this kilometre, in seconds.</summary>
    public required double SecondsA { get; init; }

    public required double SecondsB { get; init; }

    /// <summary>Positive means the compared trip took longer on this kilometre.</summary>
    public required double DeltaSeconds { get; init; }

    public required double ElevationGainA { get; init; }

    public required double ElevationGainB { get; init; }

    /// <summary>True when either side's kilometre is a shorter remainder.</summary>
    public bool IsPartial { get; init; }
}

/// <summary>
/// The full A/B comparison, ready to be rendered: metrics, deltas per kilometre, the two pace series
/// and the headline verdict.
/// </summary>
public sealed record TripComparison
{
    /// <summary>The trip the user opened.</summary>
    public required TripComparisonSide A { get; init; }

    /// <summary>The trip it is being compared with.</summary>
    public required TripComparisonSide B { get; init; }

    /// <summary>Kilometres both trips covered, so the bars line up.</summary>
    public required IReadOnlyList<SplitDelta> Splits { get; init; }

    /// <summary>Axis captions for the pace series, e.g. "1", "2", "3".</summary>
    public required IReadOnlyList<string> DistanceAxisLabels { get; init; }

    /// <summary>Equivalent pace per kilometre of side A, in seconds. 0 for missing kilometres.</summary>
    public required IReadOnlyList<double> PaceSeriesA { get; init; }

    public required IReadOnlyList<double> PaceSeriesB { get; init; }

    /// <summary>Positive means B took longer on that kilometre.</summary>
    public required IReadOnlyList<double> SplitDeltaSeries { get; init; }

    public double DistanceDeltaMeters => B.DistanceMeters - A.DistanceMeters;

    public double MovingSecondsDelta => B.MovingSeconds - A.MovingSeconds;

    public double ElevationGainDeltaMeters => B.ElevationGainMeters - A.ElevationGainMeters;

    /// <summary>Positive means B was slower per equivalent kilometre.</summary>
    public double EquivalentPaceDeltaSecondsPerKilometer =>
        B.EquivalentPaceSecondsPerKilometer - A.EquivalentPaceSecondsPerKilometer;

    /// <summary>True when the compared trip was the faster one beyond the noise band.</summary>
    public bool BIsFaster => EquivalentPaceDeltaSecondsPerKilometer < -TripComparisonCalculator.FlatBandSecondsPerKilometer;

    public bool AIsFaster => EquivalentPaceDeltaSecondsPerKilometer > TripComparisonCalculator.FlatBandSecondsPerKilometer;

    public required string Verdict { get; init; }

    public required string VerdictDetail { get; init; }

    /// <summary>Set when the two trips did not ask for quite the same thing, e.g. more climbing.</summary>
    public string? FairnessNote { get; init; }

    public bool HasSplits => Splits.Count > 0;
}

/// <summary>
/// Turns two trips into a <see cref="TripComparison"/>. Pure and synchronous so the whole comparison
/// can be unit-tested without a database or a UI.
/// </summary>
public static class TripComparisonCalculator
{
    /// <summary>Pace difference inside this band is called equal; below it, GPS noise decides the winner.</summary>
    public const double FlatBandSecondsPerKilometer = 3.0;

    /// <summary>Climb difference relative to the shorter trip that deserves a fairness note.</summary>
    private const double FairnessRelativeElevationDifference = 0.20;

    /// <summary>Absolute climb difference that deserves a fairness note regardless of proportion.</summary>
    private const double FairnessAbsoluteElevationDifferenceMeters = 30.0;

    /// <summary>Distance spread relative to the shortest trip that makes a pace ranking misleading.</summary>
    private const double FairnessRelativeDistanceDifference = 0.15;

    /// <summary>Builds one side from a trip and its track. Uses the trip's stored values when present.</summary>
    public static TripComparisonSide CreateSide(Trip trip, IReadOnlyList<LocationModel> track)
    {
        var ordered = track.OrderBy(location => location.Timestamp).ToList();
        var smoothed = Services.Location.TrackSmoother.Smooth(ordered);

        double distance = trip.Distance is > 0
            ? trip.Distance.Value
            : Services.Location.TrackSmoother.SmoothedDistanceMeters(ordered);

        var (moving, _) = Stats.TripMetricsCalculator.ComputeMovingAndStopped(ordered);
        double elevationGain = Stats.TripMetricsCalculator.ComputeElevationGain(ordered);

        double elapsed = trip.TotalTime is > 0
            ? trip.TotalTime.Value
            : (trip.EndTime - trip.StartTime).TotalSeconds;
        elapsed = Math.Max(0, elapsed);

        double movingSeconds = trip.MovingTime is > 0 ? trip.MovingTime.Value : moving.TotalSeconds;
        if (movingSeconds <= 0)
        {
            movingSeconds = elapsed;
        }

        return new TripComparisonSide
        {
            Trip = trip,
            Track = smoothed,
            Splits = KilometerSplitCalculator.Compute(ordered),
            DistanceMeters = distance,
            MovingSeconds = movingSeconds,
            ElapsedSeconds = elapsed,
            ElevationGainMeters = elevationGain,
            MaxSpeedMetersPerSecond = trip.MaxSpeed ?? 0,
            EquivalentPaceSecondsPerKilometer = EffortModel.EquivalentPaceSecondsPerKilometer(
                distance, movingSeconds, elapsed, elevationGain),
        };
    }

    /// <summary>
    /// Compares two or more trips against the first one as the basis. The two-trip case is the classic
    /// side-by-side comparison and keeps exactly the wording and numbers it always had.
    /// </summary>
    public static TripComparisonSet CompareSet(IReadOnlyList<TripComparisonSide> sides)
    {
        ArgumentNullException.ThrowIfNull(sides);

        if (sides.Count < 2)
        {
            throw new ArgumentException("Für einen Vergleich braucht es mindestens zwei Trips.", nameof(sides));
        }

        var members = sides
            .Select((side, index) => new TripComparisonMember
            {
                Index = index,
                Side = side,
                ColorHex = TripComparisonPalette.ColorFor(index),
            })
            .ToList();

        var splits = BuildSplitMatrix(sides);
        var ranking = BuildRanking(members);

        var labels = splits
            .Select(split => split.Number.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .ToList();

        var series = members
            .Select(member => (IReadOnlyList<double>)splits
                .Select(split => split.SecondsPerMember[member.Index])
                .ToList())
            .ToList();

        return new TripComparisonSet
        {
            Members = members,
            Splits = splits,
            DistanceAxisLabels = labels,
            PaceSeries = series,
            Ranking = ranking,
            Verdict = SetVerdict(members, ranking),
            VerdictDetail = SetVerdictDetail(members, ranking),
            FairnessNote = SetFairnessNote(members),
        };
    }

    /// <summary>
    /// The classic two-trip comparison. Kept as its own shape so the side-by-side page of two trips
    /// does not have to know anything about a list of members.
    /// </summary>
    public static TripComparison Compare(TripComparisonSide a, TripComparisonSide b)
    {
        var set = CompareSet(new[] { a, b });

        var splits = new List<SplitDelta>(set.Splits.Count);
        for (int index = 0; index < set.Splits.Count; index++)
        {
            var row = set.Splits[index];

            splits.Add(new SplitDelta
            {
                Number = row.Number,
                SecondsA = row.SecondsPerMember[0],
                SecondsB = row.SecondsPerMember[1],
                DeltaSeconds = row.DeltaSecondsPerMember[1],
                ElevationGainA = a.Splits[index].ElevationGainMeters,
                ElevationGainB = b.Splits[index].ElevationGainMeters,
                IsPartial = row.IsPartial,
            });
        }

        return new TripComparison
        {
            A = a,
            B = b,
            Splits = splits,
            DistanceAxisLabels = set.DistanceAxisLabels,
            PaceSeriesA = set.PaceSeries[0],
            PaceSeriesB = set.PaceSeries[1],
            SplitDeltaSeries = splits.Select(split => split.DeltaSeconds).ToList(),
            Verdict = set.Verdict,
            VerdictDetail = set.VerdictDetail,
            FairnessNote = set.FairnessNote,
        };
    }

    /// <summary>
    /// One row per kilometre every member actually covered, so a trip that stopped after 8 km is
    /// compared over 8 km instead of having its missing kilometres counted as wins.
    /// </summary>
    private static List<SplitMatrixRow> BuildSplitMatrix(IReadOnlyList<TripComparisonSide> sides)
    {
        int shared = sides.Min(side => side.Splits.Count);
        var rows = new List<SplitMatrixRow>(shared);

        for (int index = 0; index < shared; index++)
        {
            var seconds = sides
                .Select(side => side.Splits[index].EquivalentPaceSecondsPerKilometer)
                .ToList();

            double baseline = seconds[0];
            int fastest = -1;
            double best = 0;

            for (int member = 0; member < seconds.Count; member++)
            {
                if (seconds[member] <= 0 || (fastest >= 0 && seconds[member] >= best))
                {
                    continue;
                }

                fastest = member;
                best = seconds[member];
            }

            rows.Add(new SplitMatrixRow
            {
                Number = sides[0].Splits[index].Number,
                SecondsPerMember = seconds,
                DeltaSecondsPerMember = seconds.Select(value => value - baseline).ToList(),
                IsPartial = sides.Any(side => side.Splits[index].IsPartial),
                FastestMemberIndex = fastest,
            });
        }

        return rows;
    }

    /// <summary>Fastest run first. A trip without usable pace data is ranked last, not first.</summary>
    private static List<TripComparisonRank> BuildRanking(IReadOnlyList<TripComparisonMember> members)
    {
        double baseline = members[0].EquivalentPaceSecondsPerKilometer;

        return members
            .OrderBy(member => member.EquivalentPaceSecondsPerKilometer <= 0 ? 1 : 0)
            .ThenBy(member => member.EquivalentPaceSecondsPerKilometer <= 0
                ? 0
                : member.EquivalentPaceSecondsPerKilometer)
            .Select((member, position) => new TripComparisonRank
            {
                Rank = position + 1,
                Member = member,
                DeltaSecondsPerKilometer = member.EquivalentPaceSecondsPerKilometer <= 0 || baseline <= 0
                    ? 0
                    : member.EquivalentPaceSecondsPerKilometer - baseline,
            })
            .ToList();
    }

    private static string SetVerdict(
        IReadOnlyList<TripComparisonMember> members,
        IReadOnlyList<TripComparisonRank> ranking)
    {
        if (members.Count == 2)
        {
            var baseline = members[0];
            var other = members[1];

            return Verdict(
                baseline.Side,
                other.Side,
                other.EquivalentPaceSecondsPerKilometer - baseline.EquivalentPaceSecondsPerKilometer);
        }

        if (members.Any(member => member.EquivalentPaceSecondsPerKilometer <= 0))
        {
            return "Nicht vergleichbar";
        }

        var paces = members.Select(member => member.EquivalentPaceSecondsPerKilometer).ToList();
        if (paces.Max() - paces.Min() < FlatBandSecondsPerKilometer)
        {
            return $"Alle {members.Count} Läufe praktisch gleich schnell";
        }

        return $"{ranking[0].Member.Name} war der schnellste von {members.Count} Läufen";
    }

    private static string SetVerdictDetail(
        IReadOnlyList<TripComparisonMember> members,
        IReadOnlyList<TripComparisonRank> ranking)
    {
        if (members.Count == 2)
        {
            var baseline = members[0];
            var other = members[1];

            return VerdictDetail(
                baseline.Side,
                other.Side,
                other.EquivalentPaceSecondsPerKilometer - baseline.EquivalentPaceSecondsPerKilometer);
        }

        if (members.Any(member => member.EquivalentPaceSecondsPerKilometer <= 0))
        {
            return "Für einen dieser Trips fehlen die Daten für einen Tempovergleich.";
        }

        var setBaseline = members[0];
        var fastest = ranking[0].Member;
        var slowest = ranking[ranking.Count - 1].Member;
        double lead = Math.Abs(fastest.EquivalentPaceSecondsPerKilometer - setBaseline.EquivalentPaceSecondsPerKilometer);
        double spread = slowest.EquivalentPaceSecondsPerKilometer - fastest.EquivalentPaceSecondsPerKilometer;
        string range = $"zwischen schnellstem und langsamstem Lauf liegen {spread:0} s/km";

        if (fastest.IsBaseline)
        {
            var next = ranking[1].Member;
            return $"{fastest.Name} ist Ø {lead:0} s/km schneller als der nächste Lauf ({next.Name}); {range}.";
        }

        return $"{fastest.Name} war Ø {lead:0} s/km schneller als {setBaseline.Name}; {range}.";
    }

    /// <summary>
    /// Says so when the trips in the comparison did not demand the same thing, so a faster time on a
    /// flatter or shorter route is not read as progress.
    /// </summary>
    private static string? SetFairnessNote(IReadOnlyList<TripComparisonMember> members)
    {
        if (members.Count == 2)
        {
            return FairnessNote(members[0].Side, members[1].Side);
        }

        var notes = new List<string>();

        var mostClimb = members.MaxBy(member => member.Side.ElevationGainMeters)!;
        var leastClimb = members.MinBy(member => member.Side.ElevationGainMeters)!;
        double climbDifference = mostClimb.Side.ElevationGainMeters - leastClimb.Side.ElevationGainMeters;
        double climbReference = leastClimb.Side.ElevationGainMeters;

        if (climbDifference >= FairnessAbsoluteElevationDifferenceMeters
            && (climbReference <= 0
                || climbDifference / Math.Max(climbReference, 1) >= FairnessRelativeElevationDifference))
        {
            notes.Add($"{mostClimb.Name} hatte {climbDifference:0} m mehr Höhenmeter als {leastClimb.Name} – die Paces sind entsprechend umgerechnet.");
        }

        var longest = members.MaxBy(member => member.Side.DistanceMeters)!;
        var shortest = members.MinBy(member => member.Side.DistanceMeters)!;
        double distanceDifference = longest.Side.DistanceMeters - shortest.Side.DistanceMeters;

        if (shortest.Side.DistanceMeters > 0
            && distanceDifference / shortest.Side.DistanceMeters >= FairnessRelativeDistanceDifference)
        {
            notes.Add($"Die Läufe sind unterschiedlich lang ({shortest.Side.DistanceKilometers:0.0}–{longest.Side.DistanceKilometers:0.0} km) – die Pace ist über die ganze Strecke gerechnet.");
        }

        return notes.Count == 0 ? null : string.Join(" ", notes);
    }

    private static string Verdict(TripComparisonSide a, TripComparisonSide b, double delta)
    {
        if (a.EquivalentPaceSecondsPerKilometer <= 0 || b.EquivalentPaceSecondsPerKilometer <= 0)
        {
            return "Nicht vergleichbar";
        }

        if (delta < -FlatBandSecondsPerKilometer)
        {
            return $"{b.Name} war schneller";
        }

        if (delta > FlatBandSecondsPerKilometer)
        {
            return $"{a.Name} war schneller";
        }

        return "Praktisch gleich schnell";
    }

    private static string VerdictDetail(TripComparisonSide a, TripComparisonSide b, double delta)
    {
        if (a.EquivalentPaceSecondsPerKilometer <= 0 || b.EquivalentPaceSecondsPerKilometer <= 0)
        {
            return "Für einen dieser Trips fehlen die Daten für einen Tempovergleich.";
        }

        double absolute = Math.Abs(delta);
        string perKilometer = $"{absolute:0} s/km";

        if (absolute < FlatBandSecondsPerKilometer)
        {
            return $"Unterschied von nur {perKilometer} – im Bereich der normalen Streuung.";
        }

        string faster = delta > 0 ? a.Name : b.Name;
        return $"{faster} war Ø {perKilometer} schneller (auf gleichen Höhenaufwand umgerechnet).";
    }

    /// <summary>
    /// Says so when the two trips did not demand the same thing, so a faster time on a flatter route
    /// is not read as progress.
    /// </summary>
    private static string? FairnessNote(TripComparisonSide a, TripComparisonSide b)
    {
        double difference = b.ElevationGainMeters - a.ElevationGainMeters;
        double absolute = Math.Abs(difference);
        double reference = Math.Min(a.ElevationGainMeters, b.ElevationGainMeters);

        bool notable = absolute >= FairnessAbsoluteElevationDifferenceMeters
            && (reference <= 0 || absolute / Math.Max(reference, 1) >= FairnessRelativeElevationDifference);

        if (!notable)
        {
            return null;
        }

        string more = difference > 0 ? b.Name : a.Name;
        return $"{more} hatte {absolute:0} m mehr Höhenmeter – der Tempovergleich ist entsprechend umgerechnet.";
    }
}

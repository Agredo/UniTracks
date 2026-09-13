using UniTracks.Models.Comparison;
using UniTracks.Models.Trip;

namespace UniTracks.Services.Comparison;

/// <summary>One trip that belongs to a route history, with everything the builder needs to place it.</summary>
public sealed record RouteAttemptSource
{
    public required Trip Trip { get; init; }

    public required TripFingerprint Fingerprint { get; init; }

    /// <summary>
    /// Display name of the trip's type, e.g. "Trail Run". Resolved by the caller from the catalogue,
    /// because it must not be re-read per attempt and the fingerprint only stores the identifier.
    /// </summary>
    public string? TripTypeName { get; init; }
}

/// <summary>One run of a route, placed in the sequence of all runs of that route.</summary>
public sealed record RouteAttempt
{
    public required Trip Trip { get; init; }

    public required TripFingerprint Fingerprint { get; init; }

    /// <summary>1-based position in chronological order.</summary>
    public required int Number { get; init; }

    /// <summary>Seconds per equivalent kilometre; the value attempts are ranked by.</summary>
    public required double EquivalentPaceSecondsPerKilometer { get; init; }

    /// <summary>Seconds per kilometre slower than the best attempt of the same trip type.</summary>
    public required double DeltaToBestSeconds { get; init; }

    /// <summary>Seconds per kilometre slower than the previous attempt of the same trip type.</summary>
    public required double DeltaToPreviousSeconds { get; init; }

    /// <summary>
    /// False for an attempt with a different trip type, listed only because the user asked to include
    /// all trip types. Such an attempt never becomes the best time and never drives the trend.
    /// </summary>
    public bool IsSameTripType { get; init; } = true;

    /// <summary>Display name of the attempt's trip type, e.g. "Gassi gehen".</summary>
    public string? TripTypeName { get; init; }

    public bool IsBest { get; init; }

    public bool IsFirst { get; init; }

    /// <summary>True for the trip the comparison was started from.</summary>
    public bool IsCurrent { get; init; }
}

/// <summary>
/// Every run of one route, in order — the answer to "I have run this n times, did I get better?".
/// </summary>
public sealed record RouteHistory
{
    /// <summary>Chronological, oldest first.</summary>
    public required IReadOnlyList<RouteAttempt> Attempts { get; init; }

    /// <summary>Attempts of the current trip's own type; the ones the verdicts are based on.</summary>
    public int AttemptCount => Attempts.Count(attempt => attempt.IsSameTripType);

    /// <summary>Attempts listed only because the user asked for all trip types.</summary>
    public int OtherTypeAttemptCount => Attempts.Count(attempt => !attempt.IsSameTripType);

    /// <summary>True when the list holds attempts of another trip type.</summary>
    public bool HasOtherTripTypes => OtherTypeAttemptCount > 0;

    /// <summary>Name the other attempts are shown under, e.g. "Walk".</summary>
    public string? OtherTripTypeName => Attempts
        .Where(attempt => !attempt.IsSameTripType)
        .Select(attempt => attempt.TripTypeName)
        .FirstOrDefault(name => !string.IsNullOrEmpty(name));

    /// <summary>Attempts that count towards the history, i.e. the current trip included.</summary>
    public bool HasHistory => Attempts.Count > 1;

    public RouteAttempt? First { get; init; }

    public RouteAttempt? Latest { get; init; }

    /// <summary>Fastest attempt of the current trip's own type.</summary>
    public RouteAttempt? Best { get; init; }

    /// <summary>
    /// Change in equivalent pace per 30 days, from a least-squares fit over the attempts of the
    /// current trip's own type. Negative means getting faster. Zero when there are too few of them.
    /// </summary>
    public double TrendSecondsPerKilometerPerMonth { get; init; }

    /// <summary>Short German verdict on the trend, e.g. "Wird schneller".</summary>
    public required string TrendLabel { get; init; }

    /// <summary>One line for the section header, e.g. "4× gelaufen · Bestzeit 24:12".</summary>
    public required string Headline { get; init; }

    /// <summary>
    /// Seconds per kilometre gained or lost between the first and the latest attempt of the current
    /// trip's own type.
    /// </summary>
    public double FirstToLatestSecondsPerKilometer { get; init; }
}

/// <summary>
/// Attempts on one route, assembled from the trips that matched by geometry. Pure besides nothing —
/// the caller supplies the attempts, this only orders and interprets them.
/// <para>
/// Best time, trend and the attempt-to-attempt deltas always stay within the current trip's own type.
/// Attempts of another type can be listed next to them, but they never move a personal best: a dog
/// walk on the same path is not a slow run.
/// </para>
/// </summary>
public static class RouteHistoryBuilder
{
    /// <summary>Monthly change below which the trend is called flat (in seconds per kilometre).</summary>
    public const double FlatTrendBandSecondsPerKilometerPerMonth = 2.0;

    /// <summary>Attempts needed before a trend is meaningful at all.</summary>
    public const int MinimumAttemptsForTrend = 3;

    /// <summary>
    /// Builds the history of the route <paramref name="currentTrip"/> belongs to.
    /// </summary>
    /// <param name="currentTrip">The trip the user opened.</param>
    /// <param name="currentFingerprint">
    /// Its fingerprint. Null when the track is unusable; such a trip cannot be an attempt, because an
    /// attempt without geometry has no pace to place in the sequence.
    /// </param>
    /// <param name="sameRouteTrips">The trips that matched this route by shape.</param>
    public static RouteHistory Build(
        Trip currentTrip,
        TripFingerprint? currentFingerprint,
        IReadOnlyList<RouteAttemptSource> sameRouteTrips)
    {
        var entries = new List<RouteAttemptSource>(sameRouteTrips.Count + 1);

        if (currentFingerprint is not null)
        {
            entries.Add(new RouteAttemptSource
            {
                Trip = currentTrip,
                Fingerprint = currentFingerprint,
            });
        }

        foreach (var entry in sameRouteTrips)
        {
            if (entry.Trip.ID != currentTrip.ID)
            {
                entries.Add(entry);
            }
        }

        entries = entries
            .OrderBy(entry => entry.Trip.StartTime)
            .ToList();

        if (entries.Count == 0)
        {
            return new RouteHistory
            {
                Attempts = Array.Empty<RouteAttempt>(),
                TrendLabel = string.Empty,
                Headline = "Noch keine Vergleichsfahrten",
            };
        }

        string currentIdentifier = currentFingerprint?.TripIdentifier ?? string.Empty;

        var paced = entries
            .Select((entry, index) => new
            {
                Entry = entry,
                Index = index,
                IsSameTripType = IsSameTripType(currentIdentifier, entry.Fingerprint),
                Pace = EffortModel.EquivalentPaceSecondsPerKilometer(
                    entry.Fingerprint.DistanceMeters,
                    entry.Fingerprint.MovingSeconds,
                    entry.Fingerprint.ElapsedSeconds,
                    entry.Fingerprint.ElevationGainMeters),
            })
            .ToList();

        double best = paced
            .Where(p => p.IsSameTripType && p.Pace > 0)
            .Select(p => p.Pace)
            .DefaultIfEmpty(0)
            .Min();

        var attempts = new List<RouteAttempt>(paced.Count);
        double previousSameTypePace = 0;

        foreach (var p in paced)
        {
            attempts.Add(new RouteAttempt
            {
                Trip = p.Entry.Trip,
                Fingerprint = p.Entry.Fingerprint,
                Number = p.Index + 1,
                EquivalentPaceSecondsPerKilometer = p.Pace,
                DeltaToBestSeconds = best > 0 && p.Pace > 0 ? p.Pace - best : 0,
                DeltaToPreviousSeconds = previousSameTypePace > 0 && p.Pace > 0 ? p.Pace - previousSameTypePace : 0,
                IsSameTripType = p.IsSameTripType,
                TripTypeName = p.Entry.TripTypeName,
                IsBest = p.IsSameTripType && best > 0 && Math.Abs(p.Pace - best) < 0.001,
                IsFirst = p.Index == 0,
                IsCurrent = p.Entry.Trip.ID == currentTrip.ID,
            });

            if (p.IsSameTripType && p.Pace > 0)
            {
                previousSameTypePace = p.Pace;
            }
        }

        var own = attempts.Where(attempt => attempt.IsSameTripType).ToList();

        var bestAttempt = attempts.FirstOrDefault(attempt => attempt.IsBest);
        var first = attempts[0];
        var latest = attempts[^1];

        double trend = Trend(own);
        double firstToLatest = own.Count >= 2
            && own[0].EquivalentPaceSecondsPerKilometer > 0
            && own[^1].EquivalentPaceSecondsPerKilometer > 0
                ? own[^1].EquivalentPaceSecondsPerKilometer - own[0].EquivalentPaceSecondsPerKilometer
                : 0;

        return new RouteHistory
        {
            Attempts = attempts,
            First = first,
            Latest = latest,
            Best = bestAttempt,
            TrendSecondsPerKilometerPerMonth = trend,
            TrendLabel = TrendLabel(own.Count, trend),
            Headline = Headline(own.Count, bestAttempt, attempts.Count - own.Count),
            FirstToLatestSecondsPerKilometer = firstToLatest,
        };
    }

    /// <summary>
    /// Two attempts share a type when their identifiers match. A trip without a type cannot be told
    /// apart from anything, so it never splits the history — otherwise an unlabelled library would
    /// report every attempt as a foreign type.
    /// </summary>
    private static bool IsSameTripType(string currentIdentifier, TripFingerprint candidate)
    {
        if (string.IsNullOrEmpty(currentIdentifier) || string.IsNullOrEmpty(candidate.TripIdentifier))
        {
            return true;
        }

        return string.Equals(currentIdentifier, candidate.TripIdentifier, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Least-squares slope of equivalent pace against time, normalized to 30 days.</summary>
    private static double Trend(IReadOnlyList<RouteAttempt> attempts)
    {
        var usable = attempts
            .Where(attempt => attempt.EquivalentPaceSecondsPerKilometer > 0)
            .ToList();

        if (usable.Count < MinimumAttemptsForTrend)
        {
            return 0;
        }

        var origin = usable[0].Trip.StartTime;
        var xs = usable.Select(attempt => (attempt.Trip.StartTime - origin).TotalDays).ToList();
        var ys = usable.Select(attempt => attempt.EquivalentPaceSecondsPerKilometer).ToList();

        double meanX = xs.Average();
        double meanY = ys.Average();

        double covariance = 0;
        double variance = 0;

        for (int i = 0; i < xs.Count; i++)
        {
            double dx = xs[i] - meanX;
            covariance += dx * (ys[i] - meanY);
            variance += dx * dx;
        }

        if (variance <= 0)
        {
            return 0;
        }

        return covariance / variance * 30.0;
    }

    private static string TrendLabel(int attemptCount, double trend)
    {
        if (attemptCount < MinimumAttemptsForTrend)
        {
            return attemptCount > 1 ? "Noch zu wenige Läufe für einen Trend" : string.Empty;
        }

        return trend switch
        {
            <= -FlatTrendBandSecondsPerKilometerPerMonth => "Wird schneller",
            >= FlatTrendBandSecondsPerKilometerPerMonth => "Wird langsamer",
            _ => "Bleibt konstant",
        };
    }

    private static string Headline(int ownTypeCount, RouteAttempt? best, int otherTypeCount)
    {
        if (ownTypeCount == 0 && otherTypeCount == 0)
        {
            return "Noch keine Vergleichsfahrten";
        }

        string count = ownTypeCount == 1 ? "1× gelaufen" : $"{ownTypeCount}× gelaufen";

        if (best is null)
        {
            return otherTypeCount > 0 ? $"{count} · +{otherTypeCount} anderer Typ" : count;
        }

        var moving = TimeSpan.FromSeconds(best.Fingerprint.MovingSeconds);
        string bestTime = moving.TotalHours >= 1
            ? moving.ToString(@"h\:mm\:ss")
            : moving.ToString(@"mm\:ss");

        string headline = $"{count} · Bestzeit {bestTime}";

        // Say out loud that the list is longer than the count, otherwise the two numbers contradict.
        return otherTypeCount > 0
            ? $"{headline} · +{otherTypeCount} anderer Typ"
            : headline;
    }
}

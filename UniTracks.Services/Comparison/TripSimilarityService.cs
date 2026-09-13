using UniTracks.Data.Repository;
using UniTracks.Models.Comparison;
using UniTracks.Models.Trip;

namespace UniTracks.Services.Comparison;

/// <summary>A matched trip, resolved to the trip itself so the UI can show its name and date.</summary>
public sealed record RelatedTrip
{
    public required Trip Trip { get; init; }

    public required TripFingerprint Fingerprint { get; init; }

    public required TripMatch Match { get; init; }
}

/// <summary>Everything the comparison page needs for one trip, in the order it shows it.</summary>
public sealed record TripComparisonReport
{
    public required Trip Trip { get; init; }

    /// <summary>Null when the trip has no usable track, e.g. a hand-entered activity.</summary>
    public TripFingerprint? Fingerprint { get; init; }

    /// <summary>Every run of the same route, including this one.</summary>
    public required RouteHistory History { get; init; }

    /// <summary>Other trips on the same route, i.e. genuine repeats that count towards the history.</summary>
    public required IReadOnlyList<RelatedTrip> SameRoute { get; init; }

    /// <summary>
    /// Trips that share ground with this one without being the same route - the same loop run in part,
    /// or the same path with a detour. Kept out of the history on purpose: a lookalike must not set the
    /// best time of this route.
    /// </summary>
    public IReadOnlyList<RelatedTrip> SimilarRoutes { get; init; } = Array.Empty<RelatedTrip>();

    /// <summary>Trips elsewhere that asked for a comparable performance.</summary>
    public required IReadOnlyList<RelatedTrip> ComparableEffort { get; init; }

    /// <summary>True while the background indexing is still working through the trip library.</summary>
    public bool IsIndexing { get; init; }

    /// <summary>
    /// The trip type filter as it was applied, so the UI can keep its toggle in sync with the results.
    /// </summary>
    public bool IncludeAllTripTypes { get; init; }

    /// <summary>
    /// Explains in one sentence which sport type the comparison is restricted to, or why there is
    /// nothing to compare. Without this an empty list just looks broken.
    /// </summary>
    public string? TypeNotice { get; init; }

    public bool HasAnything => Fingerprint is not null
        && (History.HasHistory || SimilarRoutes.Count > 0 || ComparableEffort.Count > 0);
}

/// <summary>
/// Finds the trips that belong with a given trip: the same route, and other routes that demanded a
/// comparable performance.
/// </summary>
public interface ITripSimilarityService
{
    /// <summary>
    /// Builds everything the comparison page shows for one trip.
    /// </summary>
    /// <param name="trip">The trip the user opened.</param>
    /// <param name="includeAllTripTypes">
    /// Relaxes the fine trip type filter so e.g. a Trail Run is found for a Run. The sport family is
    /// always respected, and best times and trends stay within the trip's own type.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<TripComparisonReport> BuildReportAsync(
        Trip trip,
        bool includeAllTripTypes = false,
        CancellationToken cancellationToken = default);

    /// <summary>Builds the full A/B comparison, loading both trips' tracks.</summary>
    Task<TripComparison?> CompareAsync(Trip tripA, Trip tripB, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the comparison of one basis trip against several others, for comparing the same route
    /// over months instead of only two runs at a time.
    /// </summary>
    /// <param name="basis">The trip every difference is measured against.</param>
    /// <param name="others">The trips to compare with it. Trips without usable track data are skipped.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<TripComparisonSet?> CompareSetAsync(
        Trip basis,
        IReadOnlyList<Trip> others,
        CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class TripSimilarityService : ITripSimilarityService
{
    private readonly IRepository repository;
    private readonly ITripFingerprintService fingerprints;
    private readonly ITripFingerprintBackfill backfill;
    private readonly ITripTypeCatalog tripTypes;
    private readonly TripSimilarityOptions options;

    public TripSimilarityService(
        IRepository repository,
        ITripFingerprintService fingerprints,
        ITripFingerprintBackfill backfill,
        ITripTypeCatalog tripTypes,
        TripSimilarityOptions? options = null)
    {
        this.repository = repository;
        this.fingerprints = fingerprints;
        this.backfill = backfill;
        this.tripTypes = tripTypes;
        this.options = options ?? TripSimilarityOptions.Default;
    }

    public async Task<TripComparisonReport> BuildReportAsync(
        Trip trip,
        bool includeAllTripTypes = false,
        CancellationToken cancellationToken = default)
    {
        // Nudge the one-time indexing along; a comparison right after an update would otherwise only
        // ever see the trips that happen to have been indexed already.
        backfill.EnsureStarted();

        var effective = options.WithTripTypeFilter(includeAllTripTypes);

        var current = await fingerprints.EnsureAsync(trip);

        var stored = await fingerprints.GetAllAsync();
        var tripsById = (await repository.GetAllAsync<Trip>()).ToDictionary(candidate => candidate.ID);

        cancellationToken.ThrowIfCancellationRequested();

        if (current is null)
        {
            return new TripComparisonReport
            {
                Trip = trip,
                Fingerprint = null,
                History = RouteHistoryBuilder.Build(trip, null, Array.Empty<RouteAttemptSource>()),
                SameRoute = Array.Empty<RelatedTrip>(),
                ComparableEffort = Array.Empty<RelatedTrip>(),
                IsIndexing = backfill.IsRunning,
                IncludeAllTripTypes = includeAllTripTypes,
                TypeNotice = "Für diesen Trip gibt es keine Streckendaten, deshalb lässt er sich nicht vergleichen.",
            };
        }

        var typeNames = await TypeNamesAsync();

        var sameRouteMatches = TripMatcher.FindSameRoute(current, stored, effective);

        // The score decides whether the other trip ran this route or a lookalike. Only a real repeat
        // may stand in the history and become the best time: a route that merely overlaps would
        // otherwise quietly turn a different course into a personal best.
        var identical = ResolveAll(
            sameRouteMatches.Where(match => match.Score >= TripMatcher.IdenticalRouteScore),
            tripsById);

        var similar = ResolveAll(
            sameRouteMatches.Where(match => match.Score < TripMatcher.IdenticalRouteScore),
            tripsById);

        var sameRouteIds = sameRouteMatches.Select(match => match.Candidate.TripID).ToHashSet();

        var comparable = ResolveAll(
            TripMatcher.FindComparableEffort(current, stored, sameRouteIds, effective),
            tripsById);

        var history = RouteHistoryBuilder.Build(
            trip,
            current,
            identical
                .Where(related => related.Fingerprint.TripID != trip.ID)
                .Select(related => new RouteAttemptSource
                {
                    Trip = related.Trip,
                    Fingerprint = related.Fingerprint,
                    TripTypeName = Name(typeNames, related.Fingerprint.TripIdentifier),
                })
                .ToList());

        return new TripComparisonReport
        {
            Trip = trip,
            Fingerprint = current,
            History = history,
            SameRoute = identical,
            SimilarRoutes = similar,
            ComparableEffort = comparable,
            IsIndexing = backfill.IsRunning,
            IncludeAllTripTypes = includeAllTripTypes,
            TypeNotice = BuildTypeNotice(current, includeAllTripTypes, typeNames),
        };
    }

    /// <summary>
    /// Says out loud which sport type the results are limited to, and what the toggle changed. A
    /// filter nobody can see is indistinguishable from a bug when the list comes back empty.
    /// </summary>
    private static string BuildTypeNotice(
        TripFingerprint fingerprint,
        bool includeAllTripTypes,
        IReadOnlyDictionary<string, string> typeNames)
    {
        if (string.IsNullOrEmpty(fingerprint.TripIdentifier))
        {
            // A trip recorded without a Lauftyp used to be matchable only by other untyped trips, which
            // on a normal library means by nothing at all. The filter is the way out, so it has to say
            // so - the strict wording above ("only untyped trips") named a restriction the toggle lifts.
            return includeAllTripTypes
                ? "Für diesen Trip ist kein Lauftyp gespeichert. Der Filter ist an, deshalb werden auch "
                    + "Trips mit einem Lauftyp verglichen; verglichen wird dann allein über Strecke und Leistung."
                : "Für diesen Trip ist kein Lauftyp gespeichert. Andere Trips werden nur verglichen, "
                    + "wenn sie ebenfalls keinen Typ haben — über den Filter lässt sich das aufheben.";
        }

        string own = Name(typeNames, fingerprint.TripIdentifier) ?? fingerprint.TripIdentifier;

        if (!includeAllTripTypes)
        {
            return $"Verglichen werden nur Trips vom Typ „{own}\u201c. "
                + "Über den Filter lassen sich auch die anderen Typen dieser Sportart einbeziehen.";
        }

        return $"Alle Typen der Sportart „{fingerprint.TripCategory}\u201c werden einbezogen. "
            + $"Bestzeit und Trend bleiben trotzdem auf „{own}\u201c bezogen.";
    }

    /// <summary>Identifier → display name, e.g. "trailrun" → "Trail Run".</summary>
    private async Task<IReadOnlyDictionary<string, string>> TypeNamesAsync()
    {
        var all = await tripTypes.GetAllAsync();

        var names = all.Values
            .Where(type => !string.IsNullOrEmpty(type.Identifier))
            .GroupBy(type => type.Identifier, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Name, StringComparer.OrdinalIgnoreCase);

        return names;
    }

    private static string? Name(IReadOnlyDictionary<string, string>? names, string identifier) =>
        !string.IsNullOrEmpty(identifier) && names?.TryGetValue(identifier, out var name) == true
            ? name
            : null;

    public async Task<TripComparison?> CompareAsync(Trip tripA, Trip tripB, CancellationToken cancellationToken = default)
    {
        var trackA = await LoadTrackAsync(tripA.ID, cancellationToken);
        var trackB = await LoadTrackAsync(tripB.ID, cancellationToken);

        if (trackA.Count < TripFingerprintBuilder.MinimumPointCount || trackB.Count < TripFingerprintBuilder.MinimumPointCount)
        {
            return null;
        }

        var sideA = TripComparisonCalculator.CreateSide(tripA, trackA);
        var sideB = TripComparisonCalculator.CreateSide(tripB, trackB);

        return TripComparisonCalculator.Compare(sideA, sideB);
    }

    public async Task<TripComparisonSet?> CompareSetAsync(
        Trip basis,
        IReadOnlyList<Trip> others,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(basis);
        ArgumentNullException.ThrowIfNull(others);

        var trips = new List<Trip> { basis };

        // The basis keeps the first colour and stays index 0; everything else follows by date so the
        // legend, the curves and the splits read in the order the runs happened.
        trips.AddRange(others
            .Where(trip => trip.ID != basis.ID)
            .OrderBy(trip => trip.StartTime)
            .Take(TripComparisonPalette.MaxMembers - 1));

        var sides = new List<TripComparisonSide>(trips.Count);

        foreach (var trip in trips)
        {
            var track = await LoadTrackAsync(trip.ID, cancellationToken);

            if (track.Count < TripFingerprintBuilder.MinimumPointCount)
            {
                // A single trip without track data must not blank the whole comparison — unless it is
                // the basis, because there is nothing left to compare against then.
                if (trip.ID == basis.ID)
                {
                    return null;
                }

                continue;
            }

            sides.Add(TripComparisonCalculator.CreateSide(trip, track));
        }

        return sides.Count < 2 ? null : TripComparisonCalculator.CompareSet(sides);
    }

    private static RelatedTrip? Resolve(TripMatch match, IReadOnlyDictionary<Guid, Trip> tripsById) =>
        tripsById.TryGetValue(match.Candidate.TripID, out var trip)
            ? new RelatedTrip { Trip = trip, Fingerprint = match.Candidate, Match = match }
            : null;

    /// <summary>
    /// Matches resolved to their trips. A match whose trip has meanwhile been deleted is dropped
    /// instead of failing the whole comparison.
    /// </summary>
    private static List<RelatedTrip> ResolveAll(
        IEnumerable<TripMatch> matches,
        IReadOnlyDictionary<Guid, Trip> tripsById) => matches
            .Select(match => Resolve(match, tripsById))
            .Where(related => related is not null)
            .Select(related => related!)
            .ToList();

    private async Task<List<Models.Location.Location>> LoadTrackAsync(Guid tripId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var locations = await repository.GetAsync<Models.Location.Location>(location => location.TripID == tripId);

        return locations.OrderBy(location => location.Timestamp).ToList();
    }
}

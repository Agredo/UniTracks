using UniTracks.Models.Comparison;
using UniTracks.Services.Comparison;

namespace UniTracks.Tests.Comparison;

/// <summary>
/// The sport type is the gate that keeps a dog walk out of a running route's history. Walk and Run
/// share the category "running", so the category alone is not enough — these tests pin both levels.
/// </summary>
public class TripMatcherTripTypeTests
{
    private static readonly TripTypeRunFixture Run = new("run", "running");
    private static readonly TripTypeRunFixture TrailRun = new("trailrun", "running");
    private static readonly TripTypeRunFixture Walk = new("walk", "running");
    private static readonly TripTypeRunFixture Cycling = new("cycling", "cycling");

    // ---- Same route ----

    [Fact]
    public void SameRoute_MatchesSameTypeOnSameGround()
    {
        var target = Fingerprint(Run, 50.0, 8.0);
        var candidate = Fingerprint(Run, 50.0, 8.0, "zweitens");

        var matches = TripMatcher.FindSameRoute(target, new[] { candidate });

        Assert.Single(matches);
        Assert.Equal(TripMatchKind.SameRoute, matches[0].Kind);
    }

    [Fact]
    public void SameRoute_RefusesWalkOnTheSamePath()
    {
        // Identical geometry: everything except the trip type would let this through. Letting a walk
        // in here would let it set the pace and the personal best of the running route.
        var run = Fingerprint(Run, 50.0, 8.0);
        var walk = Fingerprint(Walk, 50.0, 8.0, "gassi");

        Assert.Empty(TripMatcher.FindSameRoute(run, new[] { walk }));
    }

    [Fact]
    public void SameRoute_RefusesTrailRunOnTheSamePath()
    {
        var run = Fingerprint(Run, 50.0, 8.0);
        var trail = Fingerprint(TrailRun, 50.0, 8.0, "wald");

        Assert.Empty(TripMatcher.FindSameRoute(run, new[] { trail }));
    }

    [Fact]
    public void SameRoute_RefusesOtherSportFamily()
    {
        var run = Fingerprint(Run, 50.0, 8.0);
        var bike = Fingerprint(Cycling, 50.0, 8.0, "radtour");

        Assert.Empty(TripMatcher.FindSameRoute(run, new[] { bike }));
    }

    [Fact]
    public void SameRoute_CanBeRelaxedToTheSportFamily()
    {
        var options = new TripSimilarityOptions { RequireSameTripType = false };

        var run = Fingerprint(Run, 50.0, 8.0);
        var trail = Fingerprint(TrailRun, 50.0, 8.0, "wald");

        Assert.Single(TripMatcher.FindSameRoute(run, new[] { trail }, options));
    }

    [Fact]
    public void SameRoute_RelaxingStillKeepsTheFamilyGate()
    {
        var options = new TripSimilarityOptions { RequireSameTripType = false };

        var run = Fingerprint(Run, 50.0, 8.0);
        var bike = Fingerprint(Cycling, 50.0, 8.0, "radtour");

        Assert.Empty(TripMatcher.FindSameRoute(run, new[] { bike }, options));
    }

    // ---- Trips without a type ----

    [Fact]
    public void UntypedTrip_DoesNotJoinTypedRoutes()
    {
        var run = Fingerprint(Run, 50.0, 8.0);
        var untyped = Fingerprint(null, 50.0, 8.0, "ohne typ");

        Assert.Empty(TripMatcher.FindSameRoute(run, new[] { untyped }));
    }

    [Fact]
    public void UntypedTrip_StillMatchesOtherUntypedTrips()
    {
        var target = Fingerprint(null, 50.0, 8.0);
        var candidate = Fingerprint(null, 50.0, 8.0, "auch ohne typ");

        Assert.Single(TripMatcher.FindSameRoute(target, new[] { candidate }));
    }

    [Fact]
    public void UntypedTrip_CanBeMadePermissive()
    {
        var options = new TripSimilarityOptions { UnknownTripTypeMatchesAnything = true };

        var run = Fingerprint(Run, 50.0, 8.0);
        var untyped = Fingerprint(null, 50.0, 8.0, "ohne typ");

        Assert.Single(TripMatcher.FindSameRoute(run, new[] { untyped }, options));
        Assert.Single(TripMatcher.FindSameRoute(untyped, new[] { run }, options));
    }

    [Fact]
    public void Toggle_AlsoHelpsATripThatHasNoType()
    {
        // The user-visible half of the toggle. Untyped trips were the ones with nothing to compare at
        // all, and the toggle used to leave exactly those alone: it relaxed the trip type but not the
        // rule that made an untyped trip invisible to a typed one.
        var relaxed = TripSimilarityOptions.Default.WithTripTypeFilter(includeAllTripTypes: true);

        var run = Fingerprint(Run, 50.0, 8.0);
        var untyped = Fingerprint(null, 50.0, 8.0, "ohne typ");

        Assert.Empty(TripMatcher.FindSameRoute(untyped, new[] { run }));
        Assert.Single(TripMatcher.FindSameRoute(untyped, new[] { run }, relaxed));
    }

    // ---- Overlap between two cell sets ----

    [Fact]
    public void CellOverlap_IsADiceCoefficientAndNeverExceedsOne()
    {
        // Regression: the denominator used to be the union, so "shared" was counted twice and two
        // largely overlapping tracks scored up to 2.0 - which made a 500 m stretch look like most of a
        // 5 km route and turned near-misses into "Gleiche Strecke".
        var cellsA = Enumerable.Range(0, 10).Select(index => $"a{index}").ToHashSet();
        var cellsB = Enumerable.Range(0, 10).Select(index => index == 9 ? "b9" : $"a{index}").ToArray();

        // Nine of ten cells shared: 2 * 9 / (10 + 10). The old formula returned 2 * 9 / 11 = 1.64.
        Assert.Equal(0.9, TripMatcher.CellOverlap(cellsA, cellsB), 10);
    }

    [Fact]
    public void CellOverlap_PunishesAShortTripContainedInALongOne()
    {
        // A short track whose cells all lie on a much longer one covers little of it, so it must not
        // read as a repeat. Dice gives 2 * 3 / (3 + 40).
        var fullRoute = Enumerable.Range(0, 40).Select(index => $"c{index}").ToHashSet();
        var shortTrack = new[] { "c0", "c1", "c2" };

        double overlap = TripMatcher.CellOverlap(fullRoute, shortTrack);

        Assert.Equal(2.0 * 3 / 43, overlap, 10);
        Assert.InRange(overlap, 0, 1);
    }

    // ---- Comparable effort ----

    [Fact]
    public void ComparableEffort_AllowsTheSameFamilyOnDifferentGround()
    {
        // A trail run 100 km away over the same equivalent distance at the same effort is a fair
        // comparison once the user asks for all trip types of the sport.
        var options = TripSimilarityOptions.Default.WithTripTypeFilter(includeAllTripTypes: true);

        var run = FixedFingerprint(Run, 50.0, 8.0);
        var trail = FixedFingerprint(TrailRun, 51.0, 9.0, "wald");

        var matches = TripMatcher.FindComparableEffort(run, new[] { trail }, null, options);

        Assert.Single(matches);
        Assert.Equal(TripMatchKind.ComparableEffort, matches[0].Kind);
    }

    [Fact]
    public void ComparableEffort_RanksAnIdenticalTypeHigher()
    {
        var options = TripSimilarityOptions.Default.WithTripTypeFilter(includeAllTripTypes: true);

        var target = FixedFingerprint(Run, 50.0, 8.0);
        var sameType = FixedFingerprint(Run, 51.0, 9.0, "noch ein lauf");
        var otherType = FixedFingerprint(TrailRun, 52.0, 10.0, "trail");

        var matches = TripMatcher.FindComparableEffort(target, new[] { sameType, otherType }, null, options);

        Assert.Equal(2, matches.Count);
        Assert.Equal(sameType.TripID, matches[0].Candidate.TripID);
        Assert.True(matches[0].Score > matches[1].Score);
    }

    [Fact]
    public void ComparableEffort_NamesTheDeviatingType()
    {
        var options = TripSimilarityOptions.Default.WithTripTypeFilter(includeAllTripTypes: true);

        var target = FixedFingerprint(Run, 50.0, 8.0);
        var trail = FixedFingerprint(TrailRun, 51.0, 9.0, "trail");

        var matches = TripMatcher.FindComparableEffort(target, new[] { trail }, null, options);

        Assert.Contains("trailrun", matches[0].Label);
    }

    [Fact]
    public void ComparableEffort_RefusesOtherSportFamily()
    {
        var run = FixedFingerprint(Run, 50.0, 8.0);
        var bike = FixedFingerprint(Cycling, 51.0, 9.0, "radtour");

        Assert.Empty(TripMatcher.FindComparableEffort(run, new[] { bike }));
    }

    [Fact]
    public void ComparableEffort_KeepsUntypedTripsApart()
    {
        var run = FixedFingerprint(Run, 50.0, 8.0);
        var untyped = FixedFingerprint(null, 51.0, 9.0, "ohne typ");

        Assert.Empty(TripMatcher.FindComparableEffort(run, new[] { untyped }));
    }

    // ---- The "all trip types" toggle ----

    [Fact]
    public void Toggle_RelaxesSameRouteToTheFamily()
    {
        var options = TripSimilarityOptions.Default.WithTripTypeFilter(includeAllTripTypes: true);
        Assert.False(options.RequireSameTripType);

        var run = Fingerprint(Run, 50.0, 8.0);
        var trail = Fingerprint(TrailRun, 50.0, 8.0, "wald");

        Assert.Single(TripMatcher.FindSameRoute(run, new[] { trail }, options));
    }

    [Fact]
    public void Toggle_RelaxesComparableEffortToTheFamily()
    {
        var options = TripSimilarityOptions.Default.WithTripTypeFilter(includeAllTripTypes: true);

        var run = FixedFingerprint(Run, 50.0, 8.0);
        var trail = FixedFingerprint(TrailRun, 51.0, 9.0, "wald");

        Assert.Single(TripMatcher.FindComparableEffort(run, new[] { trail }, null, options));
    }

    [Fact]
    public void Toggle_StillRefusesADifferentSportFamily()
    {
        // "All trip types" must mean all types of this sport. A swim is not a slow run.
        var options = TripSimilarityOptions.Default.WithTripTypeFilter(includeAllTripTypes: true);

        var run = Fingerprint(Run, 50.0, 8.0);
        var bike = Fingerprint(Cycling, 50.0, 8.0, "radtour");

        Assert.Empty(TripMatcher.FindSameRoute(run, new[] { bike }, options));
    }

    [Fact]
    public void Toggle_OffRestoresTheStrictFilter()
    {
        var relaxed = TripSimilarityOptions.Default.WithTripTypeFilter(includeAllTripTypes: true);
        var strict = relaxed.WithTripTypeFilter(includeAllTripTypes: false);

        Assert.True(strict.RequireSameTripType);
        Assert.Equal(TripSimilarityOptions.Default.RequireSameTripType, strict.RequireSameTripType);
    }

    [Fact]
    public void ComparableEffort_RefusesTrailRunWhenTheFilterIsStrict()
    {
        // A walk at running pace and a run at walking pace score the same on effort alone, which is
        // exactly why the strict filter has to apply here too and not just on the same-route search.
        var run = FixedFingerprint(Run, 50.0, 8.0);
        var trail = FixedFingerprint(TrailRun, 51.0, 9.0, "wald");

        Assert.Empty(TripMatcher.FindComparableEffort(run, new[] { trail }));
        Assert.Empty(TripMatcher.FindComparableEffort(trail, new[] { run }));
    }

    [Fact]
    public void ComparableEffort_RefusesWalkOnIdenticalStats()
    {
        var run = FixedFingerprint(Run, 50.0, 8.0);
        var walk = FixedFingerprint(Walk, 50.0, 8.0, "gassi");

        Assert.Empty(TripMatcher.FindComparableEffort(run, new[] { walk }));
    }

    // ---- Helpers ----

    private static TripFingerprint Fingerprint(TripTypeRunFixture? type, double latitude, double longitude, string name = "ziel")
    {
        var tripId = Guid.NewGuid();
        var trip = ComparisonFixtures.Trip(
            tripId,
            name,
            ComparisonFixtures.StraightTrack(tripId, latitude, longitude, 5000),
            type?.Instance);

        return TripFingerprintBuilder.Build(trip, type?.Instance) ?? throw new InvalidOperationException("fingerprint");
    }

    private static TripFingerprint FixedFingerprint(TripTypeRunFixture? type, double latitude, double longitude, string name = "ziel")
    {
        var trip = ComparisonFixtures.Fixed(Guid.NewGuid(), name, type?.Instance, latitude, longitude, 10_000, 3000);
        return TripFingerprintBuilder.Build(trip, type?.Instance) ?? throw new InvalidOperationException("fingerprint");
    }

    /// <summary>A named trip type; a small wrapper so the tests read as the catalogue would.</summary>
    private sealed record TripTypeRunFixture(string Identifier, string Category)
    {
        public UniTracks.Models.Trip.TripType Instance { get; } = ComparisonFixtures.Type(Identifier, Category);
    }
}

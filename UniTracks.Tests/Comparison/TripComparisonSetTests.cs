using UniTracks.Data.Repository;
using UniTracks.Services.Comparison;
using UniTracks.Tests.TestSupport;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.Tests.Comparison;

/// <summary>
/// The comparison of more than two runs: every difference is measured against the basis, only the
/// kilometres all runs covered are shown, and the two-trip case keeps exactly the numbers the
/// side-by-side page always had.
/// </summary>
public class TripComparisonSetTests
{
    [Fact]
    public void ThreeRunsOfTheSameRoute_MeasureEveryDifferenceAgainstTheBasis()
    {
        // 3 km each, three clearly different paces: 333 s/km, 400 s/km and 267 s/km.
        var sides = new[]
        {
            Side("Basis", distanceMeters: 3000, movingSeconds: 1000),
            Side("Langsam", distanceMeters: 3000, movingSeconds: 1200),
            Side("Schnell", distanceMeters: 3000, movingSeconds: 800),
        };

        var set = TripComparisonCalculator.CompareSet(sides);

        Assert.Equal(3, set.Count);
        Assert.False(set.IsPair);
        Assert.True(set.HasSplits);

        // Fastest first; the basis is second here because it is not the fastest run of the three.
        Assert.Equal(new[] { "Schnell", "Basis", "Langsam" }, set.Ranking.Select(rank => rank.Member.Name));
        Assert.Equal(new[] { 1, 2, 3 }, set.Ranking.Select(rank => rank.Rank));
        Assert.True(set.Ranking[0].IsFastest);
        Assert.False(set.Ranking[0].IsBaseline);
        Assert.True(set.Ranking[1].IsBaseline);

        // Every delta is against the basis, so the basis itself reads 0 and the slower run is positive.
        Assert.Equal(0, set.Ranking[1].DeltaSecondsPerKilometer, 1);
        Assert.Equal(-66.7, set.Ranking[0].DeltaSecondsPerKilometer, 1);
        Assert.Equal(66.7, set.Ranking[2].DeltaSecondsPerKilometer, 1);

        Assert.Equal(133.3, set.PaceSpreadSecondsPerKilometer, 1);
        Assert.Equal("Schnell war der schnellste von 3 Läufen", set.Verdict);
        Assert.Contains("Schnell", set.VerdictDetail);
    }

    [Fact]
    public void SplitsStopAtTheShortestRun_AndSayWhereARunEndsEarly()
    {
        // 3.4 km, 2.8 km and 3.0 km: only the kilometres all three ran can be lined up.
        var sides = new[]
        {
            Side("Basis", distanceMeters: 3400, movingSeconds: 1200),
            Side("Kurz", distanceMeters: 2800, movingSeconds: 1000),
            Side("Genau", distanceMeters: 3000, movingSeconds: 1050),
        };

        var set = TripComparisonCalculator.CompareSet(sides);

        int shortest = sides.Min(side => side.Splits.Count);
        Assert.Equal(shortest, set.Splits.Count);
        Assert.Equal(Enumerable.Range(1, shortest), set.Splits.Select(row => row.Number));

        Assert.All(set.Splits, row => Assert.Equal(3, row.SecondsPerMember.Count));
        Assert.All(set.Splits, row => Assert.Equal(0, row.DeltaSecondsPerMember[0], 3));

        // The last shared kilometre is a remainder for the shortest run.
        Assert.True(set.Splits[^1].IsPartial);
        Assert.InRange(set.Splits[^1].FastestMemberIndex, 0, 2);

        Assert.Equal(3, set.PaceSeries.Count);
        Assert.All(set.PaceSeries, series => Assert.Equal(shortest, series.Count));
    }

    [Fact]
    public void ARunWithoutPaceDataIsRankedLast_AndBlocksTheVerdict()
    {
        var sides = new[]
        {
            Side("Basis", distanceMeters: 3000, movingSeconds: 1000),
            Side("Ohne Daten", distanceMeters: 3000, movingSeconds: 900) with
            {
                EquivalentPaceSecondsPerKilometer = 0,
            },
            Side("Zweitbeste", distanceMeters: 3000, movingSeconds: 1100),
        };

        var set = TripComparisonCalculator.CompareSet(sides);

        var last = set.Ranking[^1];
        Assert.Equal("Ohne Daten", last.Member.Name);
        Assert.Equal(0, last.DeltaSecondsPerKilometer);
        Assert.Equal("Nicht vergleichbar", set.Verdict);
        Assert.Contains("fehlen die Daten", set.VerdictDetail);

        // The run without data stays out of the spread: 333 s/km against 367 s/km for the other two.
        Assert.Equal(33.3, set.PaceSpreadSecondsPerKilometer, 1);
    }

    [Fact]
    public void MonthlyRunsOfTheSameRoute_ReportTheLeadAndTheSpread()
    {
        var sides = new[]
        {
            Side("März", distanceMeters: 5000, movingSeconds: 1500),
            Side("April", distanceMeters: 5000, movingSeconds: 1400),
            Side("Mai", distanceMeters: 5000, movingSeconds: 1300),
        };

        var set = TripComparisonCalculator.CompareSet(sides);

        Assert.Equal("Mai war der schnellste von 3 Läufen", set.Verdict);
        Assert.Contains("Mai war Ø 40 s/km schneller als März", set.VerdictDetail);
        Assert.Contains("zwischen schnellstem und langsamstem Lauf liegen 40 s/km", set.VerdictDetail);
        Assert.Null(set.FairnessNote);
    }

    [Fact]
    public void RunsOfVeryDifferentLength_GetAFairnessNote()
    {
        // 5 km, 6 km and 8 km: the longest run is a different effort, even at the same pace.
        var sides = new[]
        {
            Side("Kurz", distanceMeters: 5000, movingSeconds: 1500),
            Side("Mittel", distanceMeters: 6000, movingSeconds: 1800),
            Side("Lang", distanceMeters: 8000, movingSeconds: 2400),
        };

        var set = TripComparisonCalculator.CompareSet(sides);

        Assert.NotNull(set.FairnessNote);
        Assert.Contains("unterschiedlich lang", set.FairnessNote);
    }

    [Fact]
    public void TwoTrips_KeepExactlyTheSideBySideNumbers()
    {
        var a = Side("A", distanceMeters: 4200, movingSeconds: 1400);
        var b = Side("B", distanceMeters: 4200, movingSeconds: 1300);

        var pair = TripComparisonCalculator.Compare(a, b);
        var set = TripComparisonCalculator.CompareSet(new[] { a, b });

        // The page of two trips reads the old shape; both must agree value for value.
        Assert.Equal(pair.Splits.Count, set.Splits.Count);
        Assert.Equal(pair.PaceSeriesA, set.PaceSeries[0]);
        Assert.Equal(pair.PaceSeriesB, set.PaceSeries[1]);
        Assert.Equal(pair.Verdict, set.Verdict);
        Assert.Equal(pair.VerdictDetail, set.VerdictDetail);
        Assert.Equal(pair.FairnessNote, set.FairnessNote);
        Assert.Equal(pair.DistanceAxisLabels, set.DistanceAxisLabels);
        Assert.Equal(pair.A.EquivalentPaceSecondsPerKilometer, set.Baseline.EquivalentPaceSecondsPerKilometer);

        for (int index = 0; index < pair.Splits.Count; index++)
        {
            Assert.Equal(pair.Splits[index].Number, set.Splits[index].Number);
            Assert.Equal(pair.Splits[index].SecondsA, set.Splits[index].SecondsPerMember[0]);
            Assert.Equal(pair.Splits[index].SecondsB, set.Splits[index].SecondsPerMember[1]);
            Assert.Equal(pair.Splits[index].DeltaSeconds, set.Splits[index].DeltaSecondsPerMember[1]);
        }

        Assert.True(set.IsPair);
        Assert.Equal(1, set.Ranking[0].Rank);
    }

    [Fact]
    public void EveryMemberGetsItsOwnColour_AndTheBasisKeepsTheFirst()
    {
        var sides = Enumerable.Range(1, 4)
            .Select(index => Side($"Lauf {index}", distanceMeters: 3000, movingSeconds: 1000 + index * 30))
            .ToArray();

        var set = TripComparisonCalculator.CompareSet(sides);

        Assert.Equal(
            TripComparisonPalette.Colors.Take(4),
            set.Members.Select(member => member.ColorHex));
        Assert.Equal(TripComparisonPalette.ColorFor(0), set.Baseline.ColorHex);
        Assert.Equal(Enumerable.Range(0, 4), set.Members.Select(member => member.Index));
    }

    [Fact]
    public async Task ServiceKeepsTheBasisFirst_OrdersTheRestByDate_AndSkipsRunsWithoutTrack()
    {
        var basis = Trip("Basis", distanceMeters: 3000, movingSeconds: 1000);
        var early = Trip("Früh", distanceMeters: 3000, movingSeconds: 1100, start: ComparisonFixtures.DefaultStart.AddDays(-40));
        var late = Trip("Spät", distanceMeters: 3000, movingSeconds: 900, start: ComparisonFixtures.DefaultStart.AddDays(40));
        var empty = Trip("Ohne Track", distanceMeters: 3000, movingSeconds: 950, start: ComparisonFixtures.DefaultStart.AddDays(-10));

        var repository = new InMemoryRepository();
        Seed(repository, basis, early, late);
        repository.Seed(empty);

        var service = BuildService(repository);
        var set = await service.CompareSetAsync(
            basis,
            new[] { empty, late, early },
            TestContext.Current.CancellationToken);

        Assert.NotNull(set);
        Assert.Equal(3, set.Count);
        Assert.Equal(new[] { "Basis", "Früh", "Spät" }, set.Members.Select(member => member.Name));
        Assert.Equal(basis.ID, set.Baseline.Side.Trip.ID);
    }

    [Fact]
    public async Task ServiceReturnsNothingWhenTheBasisHasNoTrack()
    {
        var basis = Trip("Basis", distanceMeters: 3000, movingSeconds: 1000);
        var other = Trip("Anderer", distanceMeters: 3000, movingSeconds: 1100, start: ComparisonFixtures.DefaultStart.AddDays(-10));

        var repository = new InMemoryRepository();
        repository.Seed(basis);
        Seed(repository, other);

        var service = BuildService(repository);
        var set = await service.CompareSetAsync(basis, new[] { other }, TestContext.Current.CancellationToken);

        Assert.Null(set);
    }

    [Fact]
    public async Task ServiceCapsTheComparisonAtEightRuns()
    {
        var basis = Trip("Basis", distanceMeters: 3000, movingSeconds: 1000);
        var others = Enumerable.Range(1, 12)
            .Select(index => Trip(
                $"Lauf {index}",
                distanceMeters: 3000,
                movingSeconds: 1000 + index,
                start: ComparisonFixtures.DefaultStart.AddDays(index)))
            .ToArray();

        var repository = new InMemoryRepository();
        Seed(repository, basis);
        Seed(repository, others);

        var service = BuildService(repository);
        var set = await service.CompareSetAsync(basis, others, TestContext.Current.CancellationToken);

        Assert.NotNull(set);
        Assert.Equal(TripComparisonPalette.MaxMembers, set.Count);
        Assert.Equal(
            Enumerable.Range(0, TripComparisonPalette.MaxMembers),
            set.Members.Select(member => member.Index));
    }

    private static TripComparisonSide Side(string name, double distanceMeters, double movingSeconds)
    {
        var id = Guid.NewGuid();
        var trip = ComparisonFixtures.Trip(
            id,
            name,
            ComparisonFixtures.StraightTrack(
                id,
                50.0,
                8.0,
                distanceMeters,
                speedMetersPerSecond: distanceMeters / movingSeconds),
            distanceMeters: distanceMeters,
            movingSeconds: movingSeconds);

        return TripComparisonCalculator.CreateSide(trip, trip.Locations!);
    }

    private static Models.Trip.Trip Trip(
        string name,
        double distanceMeters,
        double movingSeconds,
        DateTimeOffset? start = null)
    {
        var id = Guid.NewGuid();
        var first = start ?? ComparisonFixtures.DefaultStart;

        return ComparisonFixtures.Trip(
            id,
            name,
            ComparisonFixtures.StraightTrack(
                id,
                50.0,
                8.0,
                distanceMeters,
                speedMetersPerSecond: distanceMeters / movingSeconds,
                start: first),
            distanceMeters: distanceMeters,
            movingSeconds: movingSeconds,
            start: first);
    }

    private static void Seed(InMemoryRepository repository, params Models.Trip.Trip[] trips)
    {
        foreach (var trip in trips)
        {
            repository.Seed(trip);
            repository.Seed<LocationModel>(trip.Locations!.ToArray());
        }
    }

    private static ITripSimilarityService BuildService(IRepository repository)
    {
        var catalog = new TripTypeCatalog(repository);

        return new TripSimilarityService(
            repository,
            new TripFingerprintService(repository, catalog),
            new NoBackfill(),
            catalog);
    }

    private sealed class NoBackfill : ITripFingerprintBackfill
    {
        public bool IsRunning => false;

        public event EventHandler? Completed { add { } remove { } }

        public void EnsureStarted()
        {
        }

        public Task<int> WaitAsync() => Task.FromResult(0);
    }
}

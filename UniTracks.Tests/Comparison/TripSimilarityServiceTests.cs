using UniTracks.Data.Repository;
using UniTracks.Models.Comparison;
using UniTracks.Services.Comparison;
using UniTracks.Tests.TestSupport;

namespace UniTracks.Tests.Comparison;

/// <summary>
/// The split between "Gleiche Strecke" and "Ähnliche Strecken". The fingerprints are assembled by hand
/// with exactly known cells and polylines so the scores land where the assertions expect them instead
/// of depending on the geohash cell size.
/// </summary>
public class TripSimilarityServiceTests
{
    [Fact]
    public async Task OnlyTrueRepeatsReachTheHistory_ALookalikeStaysInItsOwnSection()
    {
        // The reopened trip: a 2 km line north from a known corner.
        var targetId = Guid.NewGuid();
        var targetLine = Line(50.0, 8.0, 2000);
        var target = Fingerprint(targetId, targetLine, CellLine("target", 20));

        // The repeat: same cells, same polyline, same length - every score component is perfect.
        var repeatId = Guid.NewGuid();
        var repeat = Fingerprint(repeatId, targetLine, CellLine("target", 20));
        repeat.DistanceMeters = target.DistanceMeters;

        // The lookalike: same length, but a third of its cells are its own and it ends 150 m further
        // east. That passes every route gate while clearly staying a different course.
        var lookalikeId = Guid.NewGuid();
        var lookalike = Fingerprint(lookalikeId, Line(50.0, 8.0, 2000, driftMeters: 150), Cells(20, shared: 7));
        lookalike.DistanceMeters = target.DistanceMeters;

        var report = await BuildReportAsync(target, repeat, lookalike);

        var identical = Assert.Single(report.SameRoute);
        Assert.Equal(repeatId, identical.Trip.ID);
        Assert.True(identical.Match.Score >= TripMatcher.IdenticalRouteScore);

        var similar = Assert.Single(report.SimilarRoutes);
        Assert.Equal(lookalikeId, similar.Trip.ID);
        Assert.True(similar.Match.Score < TripMatcher.IdenticalRouteScore);

        // The route history holds the repeat and the opened trip, and nothing else - which is the point:
        // a course that is merely similar must not be able to set the best time of this route.
        Assert.Equal(2, report.History.Attempts.Count);
        Assert.Equal(2, report.History.Attempts.Select(attempt => attempt.Trip.ID).Distinct().Count());
        Assert.DoesNotContain(report.History.Attempts, attempt => attempt.Trip.ID == lookalikeId);

        Assert.True(report.HasAnything);
    }

    [Fact]
    public async Task AReopenedTripWithNoLookalikesReportsNoHistory()
    {
        var targetId = Guid.NewGuid();
        var target = Fingerprint(targetId, Line(50.0, 8.0, 2000), CellLine("lonely", 20));

        var report = await BuildReportAsync(target);

        Assert.Empty(report.SameRoute);
        Assert.Empty(report.SimilarRoutes);
        Assert.Single(report.History.Attempts);
        Assert.False(report.History.HasHistory);
        Assert.False(report.HasAnything);
    }

    private static async Task<TripComparisonReport> BuildReportAsync(params TripFingerprint[] fingerprints)
    {
        var repository = new InMemoryRepository();

        foreach (var fingerprint in fingerprints)
        {
            repository.Seed(fingerprint);
            repository.Seed(ComparisonFixtures.Trip(
                fingerprint.TripID,
                $"trip-{fingerprint.TripID:N}"[..12],
                ComparisonFixtures.StraightTrack(fingerprint.TripID, 50.0, 8.0, 2000),
                distanceMeters: fingerprint.DistanceMeters,
                movingSeconds: fingerprint.MovingSeconds));
        }

        var fingerprintService = new TripFingerprintService(repository, new TripTypeCatalog(repository));
        var service = new TripSimilarityService(
            repository,
            fingerprintService,
            new NoBackfill(),
            new TripTypeCatalog(repository));

        var opened = await repository.GetByIdAsync<Models.Trip.Trip>(fingerprints[0].TripID);

        return await service.BuildReportAsync(opened!, cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>Two ends of a line north from <paramref name="latitude"/>, optionally veering east.</summary>
    private static double[] Line(double latitude, double longitude, double lengthMeters, double driftMeters = 0)
    {
        const double MetersPerDegreeLatitude = 111320.0;
        double metersPerDegreeLongitude = MetersPerDegreeLatitude * Math.Cos(latitude * Math.PI / 180);

        return new[]
        {
            latitude,
            longitude,
            latitude + lengthMeters / MetersPerDegreeLatitude,
            longitude + driftMeters / metersPerDegreeLongitude,
        };
    }

    private static TripFingerprint Fingerprint(Guid tripId, double[] line, string[] cells)
    {
        var fingerprint = new TripFingerprint
        {
            TripID = tripId,
            Version = TripFingerprintBuilder.Version,
            TripCategory = "running",
            TripIdentifier = "run",
            PointCount = 200,
            DistanceMeters = 2000,
            MovingSeconds = 700,
            ElapsedSeconds = 720,
            AverageSpeedMetersPerSecond = 2000 / 700.0,
            RouteCells = cells,
            PolylineLatitudes = new[] { line[0], line[2] },
            PolylineLongitudes = new[] { line[1], line[3] },
        };

        return fingerprint;
    }

    private static string[] CellLine(string prefix, int count) =>
        Enumerable.Range(0, count).Select(index => $"{prefix}-{index}").ToArray();

    /// <summary>
    /// <paramref name="total"/> cells, <paramref name="shared"/> of them belonging to the target, so the
    /// Dice coefficient is exactly 2 * shared / (total + <c>cells of the target</c>).
    /// </summary>
    private static string[] Cells(int total, int shared) =>
        CellLine("target", shared)
            .Concat(Enumerable.Range(0, total - shared).Select(index => $"lookalike-{index}"))
            .ToArray();

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

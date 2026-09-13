using UniTracks.Models.Comparison;
using UniTracks.Models.Trip;
using UniTracks.Services.Comparison;

namespace UniTracks.Tests.Comparison;

/// <summary>
/// Best time and trend of a route must never mix trip types. These tests pin the rule that an
/// attempt of another type can be listed but never wins the route.
/// </summary>
public class RouteHistoryBuilderTripTypeTests
{
    private static readonly TripType Run = ComparisonFixtures.Type("run", "running");
    private static readonly TripType TrailRun = ComparisonFixtures.Type("trailrun", "running");
    private static readonly TripType Walk = ComparisonFixtures.Type("walk", "running");

    [Fact]
    public void BestTime_IgnoresASlowerAttemptOfAnotherType()
    {
        // The walk is far slower on the same path; if it counted, it would sit in the history as the
        // slowest attempt and drag the personal best out of shape.
        var run = Source(Run, "lauf", 0, movingSeconds: 1800);
        var walk = Source(Walk, "gassi", 1, movingSeconds: 3600, typeName: "Walk");

        var history = Build(run, walk);

        Assert.Equal(1, history.AttemptCount);
        Assert.Equal(1, history.OtherTypeAttemptCount);
        Assert.True(history.HasOtherTripTypes);
        Assert.Equal("Walk", history.OtherTripTypeName);
        Assert.Equal(run.Trip.ID, history.Best!.Trip.ID);
        Assert.Equal(run.Trip.ID, history.Attempts.Single(a => a.IsBest).Trip.ID);
        Assert.False(history.Attempts.Single(a => a.Trip.ID == walk.Trip.ID).IsBest);
    }

    [Fact]
    public void BestTime_IgnoresAFasterAttemptOfAnotherType()
    {
        // The dangerous direction: a "walk" recorded while actually running fast would otherwise
        // become the running route's personal best.
        var run = Source(Run, "lauf", 0, movingSeconds: 2000);
        var walk = Source(Walk, "gassi", 1, movingSeconds: 1200, typeName: "Walk");

        var history = Build(run, walk);

        Assert.Equal(run.Trip.ID, history.Best!.Trip.ID);
        Assert.True(history.Attempts.Single(a => a.Trip.ID == run.Trip.ID).IsBest);
    }

    [Fact]
    public void Deltas_AreMeasuredAgainstTheBestOfTheSameType()
    {
        var run = Source(Run, "lauf", 0, movingSeconds: 1800);
        var slowRun = Source(Run, "lauf 2", 1, movingSeconds: 1980);
        var walk = Source(Walk, "gassi", 2, movingSeconds: 900, typeName: "Walk");

        var history = Build(run, slowRun, walk);

        // The walk is by far the fastest attempt in the list, so a delta measured against the overall
        // best would be wildly negative for both runs.
        Assert.True(history.Attempts.Single(a => a.Trip.ID == slowRun.Trip.ID).DeltaToBestSeconds > 0);
        Assert.Equal(0, history.Attempts.Single(a => a.Trip.ID == run.Trip.ID).DeltaToBestSeconds, 3);
        Assert.True(history.Attempts.Single(a => a.Trip.ID == walk.Trip.ID).IsSameTripType == false);
    }

    [Fact]
    public void Trend_UsesOnlyTheSameType()
    {
        var slow = Source(Run, "lauf 1", 0, movingSeconds: 2100);
        var slower = Source(Run, "lauf 2", 1, movingSeconds: 2200);
        var slowest = Source(Run, "lauf 3", 2, movingSeconds: 2300);
        var fastWalk = Source(Walk, "gassi", 3, movingSeconds: 600, typeName: "Walk");

        var history = Build(slow, slower, slowest, fastWalk);

        // Three runs that get steadily slower: the trend has to be positive (getting slower) even
        // though the newest entry in the list is a very fast one.
        Assert.True(history.TrendSecondsPerKilometerPerMonth > 0);
        Assert.Equal("Wird langsamer", history.TrendLabel);
    }

    [Fact]
    public void AttemptCount_CountsOnlyTheSameTypeAndSaysHowManyWereLeftOut()
    {
        var run = Source(Run, "lauf", 0, movingSeconds: 1800);
        var walk = Source(Walk, "gassi", 1, movingSeconds: 3600, typeName: "Walk");

        var history = Build(run, walk);

        Assert.Equal(2, history.Attempts.Count);
        Assert.Equal(1, history.AttemptCount);
        Assert.Contains("1× gelaufen", history.Headline);
        Assert.Contains("anderer Typ", history.Headline);
    }

    [Fact]
    public void Headline_HidesTheOtherTypesWhenThereAreNone()
    {
        var first = Source(Run, "lauf 1", 0, movingSeconds: 1800);
        var second = Source(Run, "lauf 2", 1, movingSeconds: 1900);

        var history = Build(first, second);

        Assert.Equal(2, history.AttemptCount);
        Assert.DoesNotContain("anderer Typ", history.Headline);
    }

    [Fact]
    public void TrailRun_IsAlsoAForeignType()
    {
        // Trail Run shares the category but not the identifier, so it must not count either.
        var run = Source(Run, "lauf", 0, movingSeconds: 1800);
        var trail = Source(TrailRun, "wald", 1, movingSeconds: 900, typeName: "Trail Run");

        var history = Build(run, trail);

        Assert.Equal(run.Trip.ID, history.Best!.Trip.ID);
        Assert.Equal(1, history.OtherTypeAttemptCount);
        Assert.Equal("Trail Run", history.OtherTripTypeName);
    }

    [Fact]
    public void UntypedAttempts_NeverSplitTheHistory()
    {
        // A library without trip types would otherwise report every single attempt as a foreign type
        // and show an empty personal best.
        var first = ComparisonFixtures.Fixed(Guid.NewGuid(), "ohne typ 1", null, 50.0, 8.0, 5000, 1800);
        var second = ComparisonFixtures.Fixed(Guid.NewGuid(), "ohne typ 2", null, 50.0, 8.0, 5000, 1900);

        var history = RouteHistoryBuilder.Build(
            first,
            Fingerprint(first),
            new[] { new RouteAttemptSource { Trip = second, Fingerprint = Fingerprint(second) } });

        Assert.Equal(2, history.AttemptCount);
        Assert.Equal(0, history.OtherTypeAttemptCount);
        Assert.False(history.HasOtherTripTypes);
    }

    [Fact]
    public void CurrentTripWithoutAFingerprint_IsNotFakedIntoTheSequence()
    {
        var typed = Source(Run, "lauf", 0, movingSeconds: 1800);
        var bare = ComparisonFixtures.Fixed(Guid.NewGuid(), "ohne spur", Run, 50.0, 8.0, 5000, 1800);

        var history = RouteHistoryBuilder.Build(bare, null, new[] { typed });

        // Without a fingerprint there is no geometry and no pace, so there is nothing to place.
        Assert.Single(history.Attempts);
        Assert.DoesNotContain(history.Attempts, attempt => attempt.IsCurrent);
        Assert.All(history.Attempts, attempt => Assert.True(attempt.IsSameTripType));
    }

    [Fact]
    public void NoComparableTrips_ReportsThatInsteadOfAFakeBestTime()
    {
        var only = ComparisonFixtures.Fixed(Guid.NewGuid(), "einmalig", Run, 50.0, 8.0, 5000, 1800);

        var history = RouteHistoryBuilder.Build(only, Fingerprint(only), Array.Empty<RouteAttemptSource>());

        Assert.Single(history.Attempts);
        Assert.False(history.HasHistory);
        Assert.StartsWith("1× gelaufen", history.Headline);
        Assert.Empty(history.TrendLabel);
    }

    // ---- Helpers ----

    private static RouteAttemptSource Source(TripType type, string name, int dayOffset, double movingSeconds, string? typeName = null)
    {
        var trip = ComparisonFixtures.Fixed(
            Guid.NewGuid(),
            name,
            type,
            50.0,
            8.0,
            5000,
            movingSeconds,
            start: ComparisonFixtures.DefaultStart.AddDays(dayOffset));

        return new RouteAttemptSource
        {
            Trip = trip,
            Fingerprint = Fingerprint(trip),
            TripTypeName = typeName ?? type.Identifier,
        };
    }

    private static RouteHistory Build(RouteAttemptSource current, params RouteAttemptSource[] others) =>
        RouteHistoryBuilder.Build(current.Trip, current.Fingerprint, others);

    private static TripFingerprint Fingerprint(Trip trip) =>
        TripFingerprintBuilder.Build(trip) ?? throw new InvalidOperationException("fingerprint");
}

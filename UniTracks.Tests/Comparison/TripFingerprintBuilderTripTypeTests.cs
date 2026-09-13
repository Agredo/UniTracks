using UniTracks.Models.Comparison;
using UniTracks.Services.Comparison;

namespace UniTracks.Tests.Comparison;

/// <summary>
/// The fingerprint is what the comparison reads instead of the GPS points, so it has to carry the trip
/// type on its own — the document store on iOS does not resolve navigations.
/// </summary>
public class TripFingerprintBuilderTripTypeTests
{
    [Fact]
    public void Build_CopiesCategoryAndIdentifierFromTheType()
    {
        var run = ComparisonFixtures.Type("run", "running");
        var trip = Trip(run);

        var fingerprint = TripFingerprintBuilder.Build(trip, run);

        Assert.NotNull(fingerprint);
        Assert.Equal("running", fingerprint!.TripCategory);
        Assert.Equal("run", fingerprint.TripIdentifier);
        Assert.Equal(run.ID, fingerprint.TripTypeId);
    }

    [Fact]
    public void Build_FallsBackToTheTripsOwnNavigation()
    {
        var walk = ComparisonFixtures.Type("walk", "running");
        var trip = Trip(walk);

        var fingerprint = TripFingerprintBuilder.Build(trip);

        Assert.NotNull(fingerprint);
        Assert.Equal("running", fingerprint!.TripCategory);
        Assert.Equal("walk", fingerprint.TripIdentifier);
    }

    [Fact]
    public void Build_LeavesTheTypeEmptyWhenTheTripHasNone()
    {
        var trip = Trip(null);

        var fingerprint = TripFingerprintBuilder.Build(trip);

        Assert.NotNull(fingerprint);
        Assert.Equal(string.Empty, fingerprint!.TripCategory);
        Assert.Equal(string.Empty, fingerprint.TripIdentifier);
        Assert.Null(fingerprint.TripTypeId);
    }

    [Fact]
    public void Build_IgnoresTheNavigationWhenATypeIsSupplied()
    {
        // The catalogue wins: the navigation can be stale or simply absent on some stores.
        var stored = ComparisonFixtures.Type("walk", "running");
        var resolved = ComparisonFixtures.Type("run", "running");
        var trip = Trip(stored);

        var fingerprint = TripFingerprintBuilder.Build(trip, resolved);

        Assert.Equal("run", fingerprint!.TripIdentifier);
    }

    [Fact]
    public void Build_ReturnsNullWithoutATrack()
    {
        var trip = ComparisonFixtures.Trip(Guid.NewGuid(), "ohne gps", Array.Empty<Models.Location.Location>());

        Assert.Null(TripFingerprintBuilder.Build(trip));
    }

    private static Models.Trip.Trip Trip(Models.Trip.TripType? type)
    {
        var id = Guid.NewGuid();
        return ComparisonFixtures.Trip(
            id,
            "einheit",
            ComparisonFixtures.StraightTrack(id, 50.0, 8.0, 5000),
            type);
    }
}

using UniTracks.Services.Comparison;
using UniTracks.Tests.Comparison;
using UniTracks.Tests.ViewModels.Fakes;
using UniTracks.ViewModels.Pages;

namespace UniTracks.Tests.ViewModels;

public class TripChartsPageViewModelTests
{
    [Fact]
    public void SlowSwimmingTrip_KeepsItsPacePer100MetersChart()
    {
        var type = ComparisonFixtures.Type("openwaterswimming", "water sports");
        var id = Guid.NewGuid();
        var track = ComparisonFixtures.StraightTrack(
            id,
            50,
            8,
            1000,
            speedMetersPerSecond: 0.75);
        var trip = ComparisonFixtures.Trip(
            id,
            "Schwimmen",
            track,
            type,
            distanceMeters: 1000,
            movingSeconds: 1333);
        var navigation = new FakeNavigationService();
        navigation.Parameters["parameter"] = trip;

        var viewModel = new TripChartsPageViewModel(navigation);

        Assert.True(viewModel.HasPaceProfile);
        Assert.Equal("min/100 m", viewModel.PaceAxisUnit);
        Assert.All(viewModel.PaceProfile, pace => Assert.InRange(pace, 1, 599));
    }
}

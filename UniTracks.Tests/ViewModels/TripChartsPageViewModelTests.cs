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

    [Fact]
    public void ThreeKilometreRun_ListsTheBestKilometreWithTimeAndPace()
    {
        var type = ComparisonFixtures.Type("run", "running");
        var id = Guid.NewGuid();
        var track = ComparisonFixtures.StraightTrack(
            id,
            50,
            8,
            3000,
            speedMetersPerSecond: 4.0);
        var trip = ComparisonFixtures.Trip(
            id,
            "Lauf",
            track,
            type,
            distanceMeters: 3000,
            movingSeconds: 750);

        var navigation = new FakeNavigationService();
        navigation.Parameters["parameter"] = trip;

        var viewModel = new TripChartsPageViewModel(navigation);

        Assert.True(viewModel.HasBestEfforts);
        var effort = Assert.Single(viewModel.BestEfforts);
        Assert.Equal("1 km", effort.DistanceText);
        Assert.Equal("04:10", effort.TimeText);
        Assert.Equal("4:10", effort.RateText);
    }
}

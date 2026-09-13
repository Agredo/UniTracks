using UniTracks.Models.User;
using UniTracks.Services.Stats;
using UniTracks.Tests.ViewModels.Fakes;
using UniTracks.ViewModels.Pages.Tabs;

namespace UniTracks.Tests.ViewModels;

/// <summary>
/// Tests for <see cref="UserPagevViewModel"/>. The hero is the first thing the user sees after a trip,
/// so every appearance has to re-read the snapshot instead of showing the numbers of the last visit,
/// and every card has to reach its page.
/// </summary>
public sealed class UserPagevViewModelTests
{
    [Fact]
    public async Task Refresh_FillsTheHeroFromTheSnapshot()
    {
        var statistics = new FakeStatisticsService
        {
            Snapshot = new StatisticsSnapshot
            {
                Level = 7,
                CurrentStreakDays = 3,
                TotalDistanceKm = 123.45,
                WeekDistanceKm = 12.3,
                TotalTrips = 42,
                YearDistanceKm = 456.7
            }
        };

        var viewModel = Create(statistics);
        await viewModel.RefreshAsync();

        Assert.Equal("7", viewModel.LevelText);
        Assert.Equal("3", viewModel.StreakText);
        Assert.Equal("123,5", viewModel.TotalDistanceText);
        Assert.Equal("12,3", viewModel.WeekDistanceText);
        Assert.Equal("42", viewModel.TripsText);
        Assert.Equal("456,7", viewModel.YearDistanceText);
    }

    [Fact]
    public async Task Refresh_AsksForTwoWeeklyBuckets()
    {
        var statistics = new FakeStatisticsService();

        await Create(statistics).RefreshAsync();

        // The hero compares this week with the last one, nothing more.
        Assert.Equal(2, statistics.RequestedWeekCount);
    }

    [Fact]
    public async Task Refresh_SecondRun_ShowsTheNewNumbers()
    {
        var statistics = new FakeStatisticsService
        {
            Snapshot = new StatisticsSnapshot { TotalDistanceKm = 10 }
        };
        var viewModel = Create(statistics);
        await viewModel.RefreshAsync();

        statistics.Snapshot = new StatisticsSnapshot { TotalDistanceKm = 25 };
        await viewModel.RefreshAsync();

        Assert.Equal("25,0", viewModel.TotalDistanceText);
    }

    [Fact]
    public async Task Refresh_GreetsWithTheProfileName()
    {
        var repository = new InMemoryRepository();
        repository.Seed(new User { ID = Guid.NewGuid(), Name = "  Chris  " });
        var viewModel = Create(new FakeStatisticsService(), repository);

        await viewModel.RefreshAsync();

        Assert.Equal("Hallo Chris", viewModel.GreetingText);
    }

    [Fact]
    public async Task Refresh_WithoutAProfile_KeepsTheDefaultGreeting()
    {
        var viewModel = Create(new FakeStatisticsService(), new InMemoryRepository());

        await viewModel.RefreshAsync();

        Assert.Equal("Bereit für den nächsten Trip", viewModel.GreetingText);
    }

    [Theory]
    [InlineData("ProfilePage")]
    [InlineData("StatisticsPage")]
    [InlineData("SettingsPage")]
    [InlineData("FeedbackPage")]
    public async Task Cards_NavigateToTheirPage(string route)
    {
        var navigation = new FakeNavigationService();
        var viewModel = Create(new FakeStatisticsService(), navigation: navigation);

        var command = route switch
        {
            "ProfilePage" => viewModel.OpenProfileCommand,
            "StatisticsPage" => viewModel.OpenStatisticsCommand,
            "SettingsPage" => viewModel.OpenSettingsCommand,
            _ => viewModel.OpenFeedbackCommand
        };

        await command.ExecuteAsync(null);

        Assert.Equal(route, Assert.Single(navigation.Navigations).Route);
    }

    private static UserPagevViewModel Create(
        FakeStatisticsService? statisticsService = null,
        InMemoryRepository? repository = null,
        FakeNavigationService? navigation = null) =>
        new(
            navigation ?? new FakeNavigationService(),
            statisticsService ?? new FakeStatisticsService(),
            repository ?? new InMemoryRepository());
}

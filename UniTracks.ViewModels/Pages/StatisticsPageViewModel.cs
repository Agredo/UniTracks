using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using UniTracks.Models.Stats;
using UniTracks.Services.Stats;

namespace UniTracks.ViewModels.Pages;

/// <summary>
/// Progress statistics at a glance: current week vs. last week, level, weekly chart,
/// all-time records and the game highlights. Data comes from <see cref="IStatisticsService"/>
/// (aggregation) and <see cref="IGamificationService"/> (level/streak).
/// </summary>
public partial class StatisticsPageViewModel : ObservableObject
{
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    private readonly IStatisticsService statisticsService;
    private readonly IGamificationService gamificationService;

    public StatisticsPageViewModel(IStatisticsService statisticsService, IGamificationService gamificationService)
    {
        this.statisticsService = statisticsService;
        this.gamificationService = gamificationService;
        _ = LoadAsync();
    }

    [ObservableProperty]
    private string weekDistanceText = "-";

    [ObservableProperty]
    private string weekDeltaText = string.Empty;

    [ObservableProperty]
    private string weekTripsText = "-";

    [ObservableProperty]
    private string weekActiveDaysText = "-";

    [ObservableProperty]
    private string streakText = "-";

    [ObservableProperty]
    private string weekMovingTimeText = "-";

    [ObservableProperty]
    private string weekElevationText = "-";

    [ObservableProperty]
    private string yearDistanceText = "-";

    [ObservableProperty]
    private string yearTripsText = "-";

    [ObservableProperty]
    private string avgDistanceText = "-";

    [ObservableProperty]
    private string avgMovingTimeText = "-";

    [ObservableProperty]
    private string avgPaceText = "-";

    [ObservableProperty]
    private string fastestPaceText = "-";

    [ObservableProperty]
    private string mostElevationText = "-";

    [ObservableProperty]
    private string longestMovingTimeText = "-";

    [ObservableProperty]
    private string levelLabel = "Level 1";

    [ObservableProperty]
    private string xpLabel = "0 XP";

    [ObservableProperty]
    private double levelProgressFraction;

    [ObservableProperty]
    private string longestTripText = "-";

    [ObservableProperty]
    private string fastestSpeedText = "-";

    [ObservableProperty]
    private string maxAltitudeText = "-";

    [ObservableProperty]
    private string coinsEarnedText = "-";

    [ObservableProperty]
    private string defenseBestWaveText = "-";

    [ObservableProperty]
    private string defenseBestScoreText = "-";

    public ObservableCollection<ChartEntry> WeeklyChart { get; } = new();

    private async Task LoadAsync()
    {
        var snapshot = await statisticsService.GetSnapshotAsync();
        var gamification = await gamificationService.ComputeAsync();

        WeekDistanceText = snapshot.WeekDistanceKm.ToString("0.0", GermanCulture);
        WeekDeltaText = BuildDeltaText(snapshot.WeekDistanceKm - snapshot.LastWeekDistanceKm);
        WeekTripsText = snapshot.WeekTrips.ToString(GermanCulture);
        WeekActiveDaysText = snapshot.WeekActiveDays.ToString(GermanCulture);
        StreakText = gamification.CurrentStreakDays.ToString(GermanCulture);
        WeekMovingTimeText = FormatDuration(snapshot.WeekMovingTime);
        WeekElevationText = snapshot.WeekElevationGainM.ToString("0", GermanCulture);

        YearDistanceText = snapshot.YearDistanceKm.ToString("0.0", GermanCulture);
        YearTripsText = snapshot.YearTrips.ToString(GermanCulture);

        AvgDistanceText = $"{snapshot.AverageDistanceKm.ToString("0.0", GermanCulture)} km";
        AvgMovingTimeText = FormatDuration(snapshot.AverageMovingTime);
        AvgPaceText = FormatPace(snapshot.AveragePaceSecondsPerKm);

        FastestPaceText = FormatPace(snapshot.FastestPaceSecondsPerKm);
        MostElevationText = $"{snapshot.MostElevationGainM.ToString("0", GermanCulture)} m";
        LongestMovingTimeText = FormatDuration(snapshot.LongestMovingTime);

        LevelLabel = gamification.LevelLabel;
        XpLabel = gamification.XpLabel;
        LevelProgressFraction = gamification.LevelProgressFraction;

        LongestTripText = $"{snapshot.LongestTripKm.ToString("0.0", GermanCulture)} km";
        FastestSpeedText = $"{snapshot.FastestAverageSpeedKmh.ToString("0.0", GermanCulture)} km/h";
        MaxAltitudeText = $"{snapshot.MaxAltitudeM.ToString("0", GermanCulture)} m";

        CoinsEarnedText = snapshot.CoinsEarnedTotal.ToString("N0", GermanCulture);
        DefenseBestWaveText = snapshot.DefenseBestWave?.ToString(GermanCulture) ?? "-";
        DefenseBestScoreText = snapshot.DefenseBestScore?.ToString("N0", GermanCulture) ?? "-";

        WeeklyChart.Clear();
        for (int i = 0; i < snapshot.RecentWeeks.Count; i++)
        {
            var week = snapshot.RecentWeeks[i];
            WeeklyChart.Add(new ChartEntry
            {
                Label = week.WeekStart.ToString("dd.MM", GermanCulture),
                Value = week.DistanceKm,
                IsHighlighted = i == snapshot.RecentWeeks.Count - 1,
            });
        }
    }

    private static string BuildDeltaText(double deltaKm)
    {
        string sign = deltaKm >= 0 ? "+" : "−";
        return $"{sign}{Math.Abs(deltaKm).ToString("0.0", GermanCulture)} km vs. Vorwoche";
    }

    private static string FormatDuration(TimeSpan duration) =>
        duration.TotalHours >= 1
            ? duration.ToString(@"h\:mm\:ss", GermanCulture)
            : duration.ToString(@"mm\:ss", GermanCulture);

    private static string FormatPace(double? secondsPerKm)
    {
        if (secondsPerKm is not { } s || s <= 0)
        {
            return "-";
        }

        var pace = TimeSpan.FromSeconds(s);
        return $"{(int)pace.TotalMinutes}:{pace.Seconds:00} /km";
    }
}

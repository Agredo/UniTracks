using UniTracks.Data.Repository;
using UniTracks.Games.Shared.Economy;
using UniTracks.Games.Shared.Persistence;
using UniTracks.Games.TowerDefense.Persistence;
using UniTracks.Models.Trip;

namespace UniTracks.Services.Stats;

/// <summary>
/// Builds the <see cref="StatisticsSnapshot"/> from qualifying trips (same filter as
/// achievements/coins) plus the game stores. All aggregation is computed on demand —
/// nothing is persisted, so stats can never drift from the underlying data.
/// </summary>
public class StatisticsService : IStatisticsService
{
    private readonly IRepository repository;
    private readonly IActivityStatsSource activityStats;
    private readonly ITowerDefenseStore towerDefenseStore;

    public StatisticsService(IRepository repository, IActivityStatsSource activityStats, ITowerDefenseStore towerDefenseStore)
    {
        this.repository = repository;
        this.activityStats = activityStats;
        this.towerDefenseStore = towerDefenseStore;
    }

    public async Task<StatisticsSnapshot> GetSnapshotAsync(int weekCount = 8)
    {
        // Locations are needed for moving time and elevation gain, so load them eagerly.
        var trips = (await repository.GetAllAsync<Trip>(trip => trip.Locations))
            .Where(TripQualification.IsQualifying)
            .ToList();

        var stats = await activityStats.GetAsync();
        var record = await towerDefenseStore.LoadRecordAsync();

        // The aggregation below is CPU-bound (per-trip moving time/elevation and many LINQ
        // passes). Run it on a worker thread so the UI thread stays responsive while the
        // statistics page appears — otherwise the page switch blocks mid-render.
        return await Task.Run(() => BuildSnapshot(trips, stats, record, weekCount));
    }

    /// <summary>Pure in-memory aggregation over already-loaded trips — safe to run off the UI thread.</summary>
    private static StatisticsSnapshot BuildSnapshot(
        List<Trip> trips, ActivityStats stats, DefenseRecord? record, int weekCount)
    {
        // Per-trip derived metrics, keyed by trip id.
        var metrics = trips.ToDictionary(
            t => t.ID,
            t =>
            {
                var ordered = t.Locations?.OrderBy(l => l.Timestamp).ToList();
                if (ordered is null || ordered.Count < 2)
                {
                    return (Moving: t.EndTime - t.StartTime, ElevationGain: 0.0);
                }

                var (moving, _) = TripMetricsCalculator.ComputeMovingAndStopped(ordered);
                return (Moving: moving > TimeSpan.Zero ? moving : t.EndTime - t.StartTime,
                    ElevationGain: TripMetricsCalculator.ComputeElevationGain(ordered));
            });

        var currentWeekStart = StartOfWeek(DateTime.Today);
        var lastWeekStart = currentWeekStart.AddDays(-7);
        var oldestWeekStart = currentWeekStart.AddDays(-7 * (weekCount - 1));
        var yearStart = new DateTime(DateTime.Today.Year, 1, 1);

        var currentWeekTrips = trips
            .Where(t => t.StartTime.LocalDateTime.Date >= currentWeekStart)
            .ToList();
        var lastWeekTrips = trips
            .Where(t => t.StartTime.LocalDateTime.Date >= lastWeekStart && t.StartTime.LocalDateTime.Date < currentWeekStart)
            .ToList();
        var yearTrips = trips
            .Where(t => t.StartTime.LocalDateTime.Date >= yearStart)
            .ToList();

        var weeks = new List<WeeklyDistance>(weekCount);
        for (int i = 0; i < weekCount; i++)
        {
            var weekStart = oldestWeekStart.AddDays(7 * i);
            var weekEnd = weekStart.AddDays(7);
            double km = trips
                .Where(t => t.StartTime.LocalDateTime.Date >= weekStart && t.StartTime.LocalDateTime.Date < weekEnd)
                .Sum(t => t.Distance ?? 0) / 1000.0;
            weeks.Add(new WeeklyDistance { WeekStart = weekStart, DistanceKm = km });
        }

        int coinsEarned = CoinEconomy.ComputeEarned(stats.Trips, stats.Xp, stats.UnlockedAchievements);

        // Pace (s/km) per trip — only meaningful for trips with real distance and time.
        var tripPaces = trips
            .Select(t => (DistanceKm: (t.Distance ?? 0) / 1000.0, Moving: metrics[t.ID].Moving))
            .Where(x => x.DistanceKm >= 0.1 && x.Moving.TotalSeconds > 0)
            .Select(x => x.Moving.TotalSeconds / x.DistanceKm)
            .ToList();

        double totalMovingSeconds = metrics.Values.Sum(m => m.Moving.TotalSeconds);

        return new StatisticsSnapshot
        {
            WeekDistanceKm = Math.Round(currentWeekTrips.Sum(t => t.Distance ?? 0) / 1000.0, 1),
            LastWeekDistanceKm = Math.Round(lastWeekTrips.Sum(t => t.Distance ?? 0) / 1000.0, 1),
            WeekTrips = currentWeekTrips.Count,
            WeekActiveDays = currentWeekTrips.Select(t => t.StartTime.LocalDateTime.Date).Distinct().Count(),
            WeekMovingTime = TimeSpan.FromSeconds(currentWeekTrips.Sum(t => metrics[t.ID].Moving.TotalSeconds)),
            WeekElevationGainM = Math.Round(currentWeekTrips.Sum(t => metrics[t.ID].ElevationGain)),
            TotalDistanceKm = Math.Round(trips.Sum(t => t.Distance ?? 0) / 1000.0, 1),
            TotalTrips = trips.Count,
            YearDistanceKm = Math.Round(yearTrips.Sum(t => t.Distance ?? 0) / 1000.0, 1),
            YearTrips = yearTrips.Count,
            AverageDistanceKm = Math.Round(trips.Select(t => t.Distance ?? 0).DefaultIfEmpty(0).Average() / 1000.0, 1),
            AverageMovingTime = trips.Count > 0
                ? TimeSpan.FromSeconds(totalMovingSeconds / trips.Count)
                : TimeSpan.Zero,
            AveragePaceSecondsPerKm = tripPaces.Count > 0 ? tripPaces.Average() : null,
            LongestTripKm = Math.Round(trips.Select(t => t.Distance ?? 0).DefaultIfEmpty(0).Max() / 1000.0, 1),
            FastestAverageSpeedKmh = Math.Round(trips.Select(t => t.AverageSpeed ?? 0).DefaultIfEmpty(0).Max() * 3.6, 1),
            MaxAltitudeM = Math.Round(trips.Select(t => t.MaxAltitude ?? 0).DefaultIfEmpty(0).Max()),
            FastestPaceSecondsPerKm = tripPaces.Count > 0 ? tripPaces.Min() : null,
            MostElevationGainM = Math.Round(metrics.Values.Select(m => m.ElevationGain).DefaultIfEmpty(0).Max()),
            LongestMovingTime = TimeSpan.FromSeconds(metrics.Values.Select(m => m.Moving.TotalSeconds).DefaultIfEmpty(0).Max()),
            RecentWeeks = weeks,
            CoinsEarnedTotal = coinsEarned,
            DefenseBestWave = record?.BestWave,
            DefenseBestScore = record?.BestScore,
        };
    }

    /// <summary>Monday of the week containing <paramref name="date"/> (German convention).</summary>
    private static DateTime StartOfWeek(DateTime date)
    {
        int daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-daysSinceMonday);
    }
}

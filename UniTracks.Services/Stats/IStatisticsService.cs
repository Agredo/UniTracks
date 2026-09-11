namespace UniTracks.Services.Stats;

/// <summary>Kilometres of one calendar week (Monday-start).</summary>
public record WeeklyDistance
{
    public DateTime WeekStart { get; init; }

    public double DistanceKm { get; init; }
}

/// <summary>
/// Aggregated progress snapshot for the statistics page: current week vs. last week,
/// all-time records, the last weeks as chart buckets, the level/streak state and the
/// game-side highlights.
/// </summary>
public record StatisticsSnapshot
{
    public double WeekDistanceKm { get; init; }

    public double LastWeekDistanceKm { get; init; }

    public int WeekTrips { get; init; }

    public int WeekActiveDays { get; init; }

    /// <summary>Moving time across this week's trips (Strava-style headline metric).</summary>
    public TimeSpan WeekMovingTime { get; init; }

    /// <summary>Climbed metres across this week's trips.</summary>
    public double WeekElevationGainM { get; init; }

    public double TotalDistanceKm { get; init; }

    public int TotalTrips { get; init; }

    /// <summary>Distance covered in the current calendar year.</summary>
    public double YearDistanceKm { get; init; }

    public int YearTrips { get; init; }

    public double AverageDistanceKm { get; init; }

    /// <summary>Average moving time per trip (trips without GPS points fall back to trip duration).</summary>
    public TimeSpan AverageMovingTime { get; init; }

    /// <summary>Average pace in seconds per kilometre; null when no trip has usable data.</summary>
    public double? AveragePaceSecondsPerKm { get; init; }

    public double LongestTripKm { get; init; }

    public double FastestAverageSpeedKmh { get; init; }

    public double MaxAltitudeM { get; init; }

    /// <summary>Best pace over a single trip (s/km); null without usable GPS data.</summary>
    public double? FastestPaceSecondsPerKm { get; init; }

    /// <summary>Most climbed metres within a single trip.</summary>
    public double MostElevationGainM { get; init; }

    /// <summary>Longest moving time of a single trip.</summary>
    public TimeSpan LongestMovingTime { get; init; }

    /// <summary>Weekly kilometres, oldest first, ending with the current week.</summary>
    public IReadOnlyList<WeeklyDistance> RecentWeeks { get; init; } = Array.Empty<WeeklyDistance>();

    /// <summary>Lifetime coins earned from activity (before any spending).</summary>
    public int CoinsEarnedTotal { get; init; }

    // Gamification state, computed from the same trip scan. Carried in the snapshot so the
    // statistics page does not need a second pass over the recorded trips.
    public int Xp { get; init; }

    public int Level { get; init; }

    public double LevelProgressFraction { get; init; }

    /// <summary>Consecutive active days ending today or yesterday; 0 when the streak is broken.</summary>
    public int CurrentStreakDays { get; init; }

    public int? DefenseBestWave { get; init; }

    public int? DefenseBestScore { get; init; }
}

/// <summary>Computes aggregated progress statistics from trips and game state.</summary>
public interface IStatisticsService
{
    /// <param name="weekCount">Number of weekly buckets (including the current week) to return.</param>
    Task<StatisticsSnapshot> GetSnapshotAsync(int weekCount = 8);
}

namespace UniTracks.Games.Persistence;

/// <summary>Aggregated activity numbers feeding the coin economy.</summary>
public record ActivityStats
{
    /// <summary>All recorded trips with their type info — drives per-type distance factors.</summary>
    public IReadOnlyList<TripActivity> Trips { get; init; } = Array.Empty<TripActivity>();

    /// <summary>Gamification XP (⌊km⌋ + 2×trips) — determines level and level-up coin bonuses.</summary>
    public int Xp { get; init; }

    /// <summary>Number of unlocked achievements (base coin reward).</summary>
    public int UnlockedAchievements { get; init; }

    /// <summary>Ids of unlocked achievements — unlocks exclusive buildings.</summary>
    public IReadOnlyList<string> UnlockedAchievementIds { get; init; } = Array.Empty<string>();

    /// <summary>Computed gamification level (XP/100 + 1).</summary>
    public int Level => Xp / 100 + 1;

    public double TotalDistanceKm => Trips.Sum(t => t.DistanceKm);

    public int TotalTrips => Trips.Count;
}

/// <summary>
/// Port that supplies lifetime activity stats (trips, distance, achievements).
/// Implemented in UniTracks.Services on top of the trip repository + gamification service.
/// </summary>
public interface IActivityStatsSource
{
    Task<ActivityStats> GetAsync();
}

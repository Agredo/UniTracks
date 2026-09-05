namespace UniTracks.Games.Persistence;

/// <summary>Aggregated activity numbers feeding the coin economy.</summary>
public record ActivityStats
{
    public double TotalDistanceKm { get; init; }

    public int TotalTrips { get; init; }

    public int UnlockedAchievements { get; init; }
}

/// <summary>
/// Port that supplies lifetime activity stats (trips, distance, achievements).
/// Implemented in UniTracks.Services on top of the trip repository + gamification service.
/// </summary>
public interface IActivityStatsSource
{
    Task<ActivityStats> GetAsync();
}

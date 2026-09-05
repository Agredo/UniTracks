using UniTracks.Games.Persistence;
using UniTracks.Services.Stats;

namespace UniTracks.Services.Game;

/// <summary>
/// Adapts the gamification computation to the games layer: coins are earned from
/// distance, trips and unlocked achievements — always computed, never stored.
/// </summary>
public class CoinService : ICoinService
{
    private readonly IGamificationService gamificationService;

    public CoinService(IGamificationService gamificationService)
    {
        this.gamificationService = gamificationService;
    }

    public async Task<ActivityStats> GetAsync()
    {
        var stats = await gamificationService.ComputeAsync();
        return new ActivityStats
        {
            TotalDistanceKm = stats.TotalDistanceKm,
            TotalTrips = stats.TotalTrips,
            UnlockedAchievements = stats.Achievements.Count(a => a.IsUnlocked),
        };
    }
}

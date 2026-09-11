using UniTracks.Data.Repository;
using UniTracks.Games.Shared.Persistence;
using UniTracks.Models.Trip;
using UniTracks.Services.Stats;

namespace UniTracks.Services.Game;

/// <summary>
/// Adapts the trip repository + gamification computation to the games layer: coins are
/// earned from distance (scaled per trip type), trips, level-ups and achievements —
/// always computed, never stored.
/// </summary>
public class CoinService : ICoinService
{
    private readonly IRepository repository;
    private readonly IGamificationService gamificationService;

    public CoinService(IRepository repository, IGamificationService gamificationService)
    {
        this.repository = repository;
        this.gamificationService = gamificationService;
    }

    public async Task<ActivityStats> GetAsync()
    {
        // The repository serves this full-table read from its read cache, so the coin balance no
        // longer triggers its own trip scan next to the statistics page's read.
        var trips = (await repository.GetAllAsync<Trip>(t => t.TripType!))
            .Where(TripQualification.IsQualifying)
            .ToList();
        var stats = await gamificationService.ComputeAsync(trips);

        // Projecting the loaded trips into the coin feed is cheap in-memory work over a handful
        // of trips; the expensive part (loading the trips) has already happened above.
        return new ActivityStats
        {
            Trips = trips.Select(t => new TripActivity
            {
                DistanceKm = (t.Distance ?? 0) / 1000.0,
                Category = t.TripType?.Category,
                Identifier = t.TripType?.Identifier,
            }).ToList(),
            Xp = stats.Xp,
            UnlockedAchievements = stats.Achievements.Count(a => a.IsUnlocked),
            UnlockedAchievementIds = stats.Achievements.Where(a => a.IsUnlocked).Select(a => a.Id).ToList(),
        };
    }
}
using UniTracks.Games.TowerDefense;

namespace UniTracks.Services.Game;

/// <summary>
/// Coordinates the trail-defense game: persistent profile (coins, unlocks, best result),
/// coin-validated tower unlocks and highscore bookkeeping. The run simulation itself
/// is pure engine (<see cref="DefenseEngine"/>) and lives in the view layer.
/// </summary>
public interface ITowerDefenseService
{
    /// <summary>Loads the persistent profile: spendable coins, unlocked towers, best result.</summary>
    Task<DefenseProfile> GetProfileAsync();

    /// <summary>Unlocks a tower permanently if it is known, new and affordable with coins.</summary>
    Task<UnlockResult> TryUnlockAsync(string towerId);

    /// <summary>Persists a finished run when it beats the stored best wave or score.</summary>
    Task<DefenseProfile> SaveRunResultAsync(int clearedWave, int score);
}

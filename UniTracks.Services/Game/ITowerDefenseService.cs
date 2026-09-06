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

    /// <summary>
    /// Loads the persisted in-progress run (towers + wave) with energy freshly recomputed
    /// from the player's current activity, or <c>null</c> when no run is stored.
    /// </summary>
    Task<DefenseState?> LoadRunAsync();

    /// <summary>Persists the in-progress run so the player can continue next time.</summary>
    Task SaveRunAsync(DefenseState state);

    /// <summary>Clears the stored in-progress run (for a fresh restart).</summary>
    Task ClearRunAsync();
}

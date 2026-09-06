namespace UniTracks.Games.TowerDefense.Persistence;

/// <summary>
/// Persistence port for the trail defense game. Implemented in UniTracks.Services on
/// top of the provider-agnostic <c>IRepository</c> (EF Core + SQLite, LiteDB on iOS).
/// </summary>
public interface ITowerDefenseStore
{
    Task<IReadOnlyList<TowerUnlock>> LoadUnlocksAsync();

    Task SaveUnlockAsync(TowerUnlock unlock);

    Task<DefenseRecord?> LoadRecordAsync();

    Task SaveRecordAsync(DefenseRecord record);

    /// <summary>Loads the persisted in-progress run, or <c>null</c> when none is stored.</summary>
    Task<DefenseRunProgress?> LoadRunAsync();

    /// <summary>Persists the in-progress run (inserts or updates the single stored row).</summary>
    Task SaveRunAsync(DefenseRunProgress run);

    /// <summary>Deletes the persisted in-progress run.</summary>
    Task ClearRunAsync();
}

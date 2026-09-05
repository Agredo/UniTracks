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
}

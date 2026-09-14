namespace UniTracks.Games.BaseCamp.Persistence;

/// <summary>
/// Persistence port for the base-camp game. Implemented in UniTracks.Services on top of
/// the provider-agnostic <c>IRepository</c> (EF Core + SQLite, LiteDB on iOS).
/// </summary>
public interface ICampStore
{
    Task<IReadOnlyList<CampModule>> LoadModulesAsync();

    /// <summary>Persists a module level (inserts or updates the single row for that module).</summary>
    Task SaveModuleAsync(CampModule module);

    /// <summary>Loads the single harvest log, or <c>null</c> on the very first visit.</summary>
    Task<CampLog?> LoadLogAsync();

    /// <summary>Persists the harvest log (inserts or updates the single stored row).</summary>
    Task SaveLogAsync(CampLog log);
}

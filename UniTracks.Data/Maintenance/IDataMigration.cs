namespace UniTracks.Data.Maintenance;

/// <summary>
/// A one-off data migration that changes how a store lays its data out. Implementations are
/// registered where the affected store is used and run in the background once the first page is on
/// screen, so a migration never delays the start of the app or competes with the first list read.
/// </summary>
public interface IDataMigration
{
    /// <summary>
    /// Performs the migration. Must be idempotent: it runs on every start and has to be a no-op once
    /// the data is already in the new layout.
    /// </summary>
    Task RunAsync();
}

namespace UniTracks.Services.Data;

/// <summary>
/// Carries the outcome of the staged database operation that
/// <see cref="IDatabaseMaintenance.ApplyPending"/> performed during startup to the UI. The swap
/// happens while the app is still building its services, so the message cannot be shown right away
/// and is handed over to the shell instead.
/// </summary>
public sealed class StartupDatabaseReport
{
    public DatabaseOperationResult Result { get; private set; } = new(false, null);

    public void Record(DatabaseOperationResult result) => Result = result;
}

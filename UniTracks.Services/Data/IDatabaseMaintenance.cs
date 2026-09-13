namespace UniTracks.Services.Data;

/// <summary>Outcome of applying a staged database operation (import or reset) at startup.</summary>
public record DatabaseOperationResult(bool Applied, string? Message);

/// <summary>
/// Database file maintenance: share it, replace it with an imported copy, or delete it. All of this
/// happens on the file level, because the app's repository holds the open handle (LiteDB on iOS,
/// EF Core + SQLite everywhere else) for the whole session.
///
/// Import and reset are therefore <em>staged</em>: the app only writes a marker file here and the
/// swap happens on the next start, before the repository opens the database. Nothing in between
/// touches the running instance.
/// </summary>
public interface IDatabaseMaintenance
{
    /// <summary>Full path of the database file the running platform uses.</summary>
    string DatabasePath { get; }

    string DatabaseFileName { get; }

    /// <summary>Size of the database file in bytes; 0 while the file does not exist yet.</summary>
    long DatabaseSizeBytes { get; }

    /// <summary>True when an import or a reset waits for the next app start.</summary>
    bool HasPendingOperation { get; }

    /// <summary>User-facing description of the staged operation; empty when there is none.</summary>
    string PendingOperationText { get; }

    /// <summary>When the last import was taken over (local time), or null.</summary>
    DateTimeOffset? LastImport { get; }

    /// <summary>
    /// Validates the picked file and copies it next to the database as the pending import.
    /// Returns null on success, otherwise a user-facing error message.
    /// </summary>
    Task<string?> StageImportAsync(string? sourceFilePath);

    /// <summary>
    /// Marks the database for deletion on the next start (everything in it is gone).
    /// Returns null on success, otherwise a user-facing error message.
    /// </summary>
    Task<string?> StageResetAsync();

    /// <summary>
    /// Writes a copy of the current database to the app's data folder, so the share sheet never
    /// touches the file the repository keeps open. Returns the full path, or null when no database
    /// exists yet or the copy failed.
    /// </summary>
    Task<string?> CreateExportCopyAsync();

    /// <summary>
    /// Applies a staged operation. Called at startup <em>before</em> the repository is created;
    /// never throws so a broken file cannot keep the app from launching.
    /// </summary>
    DatabaseOperationResult ApplyPending();
}

using System.Text;
using AgredoApplication.MVVM.Services.Abstractions.IO;
using UniTracks.Models.Constants;

namespace UniTracks.Services.Data;

/// <summary>
/// File-level maintenance of the app's database. See <see cref="IDatabaseMaintenance"/> for why
/// import and reset are staged until the next app start.
/// </summary>
public sealed class DatabaseMaintenance : IDatabaseMaintenance
{
    /// <summary>Marker written next to the database when an import waits for the next start.</summary>
    public const string ImportPendingSuffix = ".import-pending";

    /// <summary>Marker written next to the database when a reset waits for the next start.</summary>
    public const string ResetPendingSuffix = ".reset-pending";

    /// <summary>Holds the timestamp of the last applied import; also the "operation done" record.</summary>
    public const string LastImportFileName = "last-import.txt";

    private const int HeaderLength = 64;
    private const int KeepBackupCount = 3;

    private static readonly string[] SidecarSuffixes = ["-wal", "-shm", "-log"];

    private readonly IFileSystem fileSystem;
    private readonly bool useLiteDatabase;

    /// <param name="useLiteDatabase">
    /// True on iOS (LiteDB), false where the store is EF Core + SQLite. Decides which file is
    /// maintained and which file header an import has to carry.
    /// </param>
    public DatabaseMaintenance(IFileSystem fileSystem, bool useLiteDatabase)
    {
        this.fileSystem = fileSystem;
        this.useLiteDatabase = useLiteDatabase;
    }

    /// <inheritdoc />
    public string DatabaseFileName => useLiteDatabase
        ? ApplicationConstants.LiteDBName
        : ApplicationConstants.SQliteDatabaseName;

    /// <inheritdoc />
    public string DatabasePath => Path.Combine(fileSystem.AppDataDirectory, DatabaseFileName);

    private string ImportPendingPath => DatabasePath + ImportPendingSuffix;

    private string ResetPendingPath => DatabasePath + ResetPendingSuffix;

    private string LastImportPath => Path.Combine(fileSystem.AppDataDirectory, LastImportFileName);

    /// <inheritdoc />
    public long DatabaseSizeBytes
    {
        get
        {
            try
            {
                var file = new FileInfo(DatabasePath);
                return file.Exists ? file.Length : 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }

    /// <inheritdoc />
    public bool HasPendingOperation => File.Exists(ImportPendingPath) || File.Exists(ResetPendingPath);

    /// <inheritdoc />
    public string PendingOperationText
    {
        get
        {
            if (File.Exists(ResetPendingPath))
            {
                return "Vorgemerkt: alle Daten werden beim nächsten Start der App gelöscht.";
            }

            return File.Exists(ImportPendingPath)
                ? "Vorgemerkt: der Import wird beim nächsten Start der App übernommen."
                : string.Empty;
        }
    }

    /// <inheritdoc />
    public DateTimeOffset? LastImport
    {
        get
        {
            try
            {
                return File.Exists(LastImportPath) && DateTimeOffset.TryParse(File.ReadAllText(LastImportPath), out var timestamp)
                    ? timestamp
                    : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    /// <inheritdoc />
    public async Task<string?> StageImportAsync(string? sourceFilePath)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            return "Es wurde keine Datei ausgewählt.";
        }

        try
        {
            if (!File.Exists(sourceFilePath))
            {
                return "Die ausgewählte Datei wurde nicht gefunden.";
            }

            if (!IsValidDatabaseFile(sourceFilePath))
            {
                var expected = useLiteDatabase ? "LiteDB-Datei (iOS)" : "SQLite-Datei";
                return $"Die ausgewählte Datei ist keine UniTracks-Datenbank: erwartet wird eine {expected}.";
            }

            // A staged reset would delete the import again, so the newer decision wins.
            DeleteFileIfExists(ResetPendingPath);

            await using (var source = File.Open(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            await using (var target = File.Create(ImportPendingPath))
            {
                await source.CopyToAsync(target);
            }

            return null;
        }
        catch (Exception exception)
        {
            return $"Der Import konnte nicht vorbereitet werden: {exception.Message}";
        }
    }

    /// <inheritdoc />
    public Task<string?> StageResetAsync()
    {
        try
        {
            DeleteFileIfExists(ImportPendingPath);
            File.WriteAllText(ResetPendingPath, DateTimeOffset.Now.ToString("o"));
            return Task.FromResult<string?>(null);
        }
        catch (Exception exception)
        {
            return Task.FromResult<string?>($"Das Zurücksetzen konnte nicht vorgemerkt werden: {exception.Message}");
        }
    }

    /// <inheritdoc />
    public DatabaseOperationResult ApplyPending()
    {
        try
        {
            var importPending = File.Exists(ImportPendingPath);
            var resetPending = File.Exists(ResetPendingPath);

            if (!importPending && !resetPending)
            {
                return new DatabaseOperationResult(false, null);
            }

            if (importPending && !IsValidDatabaseFile(ImportPendingPath))
            {
                DeleteFileIfExists(ImportPendingPath);
                return new DatabaseOperationResult(false, "Der vorgemerkte Import war keine gültige Datenbank und wurde verworfen.");
            }

            // An open WAL/journal of the old file would be replayed over the new one, so those have
            // to go first - for the import as well as for the reset.
            DeleteSidecarFiles();

            if (importPending)
            {
                BackupExistingDatabase();
                File.Move(ImportPendingPath, DatabasePath, overwrite: true);
                File.WriteAllText(LastImportPath, DateTimeOffset.Now.ToString("o"));
                return new DatabaseOperationResult(true, "Import übernommen.");
            }

            // Reset: the database is removed so the app builds a fresh one. A dated copy stays
            // behind, because the reset is one tap away and everything would be gone for good.
            BackupExistingDatabase();
            DeleteFileIfExists(DatabasePath);
            DeleteFileIfExists(ResetPendingPath);
            File.WriteAllText(LastImportPath, string.Empty);
            return new DatabaseOperationResult(true, "Daten gelöscht.");
        }
        catch (Exception exception)
        {
            return new DatabaseOperationResult(false, $"Datenoperation fehlgeschlagen: {exception.Message}");
        }
    }

    /// <summary>
    /// Checks the file header: "SQLite format 3\0" for SQLite, the LiteDB banner for LiteDB. A file
    /// of the other kind is refused, because it could not be opened on this platform anyway.
    /// </summary>
    public bool IsValidDatabaseFile(string path)
    {
        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var header = new byte[HeaderLength];
            var read = stream.Read(header, 0, header.Length);

            if (read < 16)
            {
                return false;
            }

            if (useLiteDatabase)
            {
                return Encoding.ASCII.GetString(header, 0, read).Contains("LiteDB", StringComparison.Ordinal);
            }

            return Encoding.ASCII.GetString(header, 0, 16) == "SQLite format 3\0";
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<string?> CreateExportCopyAsync()
    {
        if (!File.Exists(DatabasePath))
        {
            return null;
        }

        try
        {
            var extension = Path.GetExtension(DatabaseFileName);
            var directory = Path.GetDirectoryName(DatabasePath) ?? fileSystem.AppDataDirectory;
            var target = Path.Combine(directory, $"UniTracks-{DateTime.Now:yyyy-MM-dd-HHmm}{extension}");

            // FileShare.ReadWrite: the repository holds the database open for the whole session.
            await using (var source = File.Open(DatabasePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            await using (var copy = File.Create(target))
            {
                await source.CopyToAsync(copy);
            }

            PruneExports(directory, extension);
            return target;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Keeps only the two newest export copies, so the folder does not fill up.</summary>
    private static void PruneExports(string directory, string extension)
    {
        try
        {
            var stale = Directory
                .GetFiles(directory, $"UniTracks-*{extension}")
                .OrderByDescending(path => path, StringComparer.Ordinal)
                .Skip(2)
                .ToList();

            foreach (var path in stale)
            {
                DeleteFileIfExists(path);
            }
        }
        catch (Exception)
        {
            // Housekeeping only.
        }
    }

    /// <summary>Keeps the current database as a dated copy next to itself before it is replaced.</summary>
    private void BackupExistingDatabase()
    {
        if (!File.Exists(DatabasePath))
        {
            return;
        }

        var backupPath = $"{DatabasePath}-backup-{DateTime.Now:yyyyMMdd-HHmmss}";
        File.Move(DatabasePath, backupPath, overwrite: true);
        PruneBackups();
    }

    /// <summary>Keeps the <see cref="KeepBackupCount"/> newest backups so the folder cannot grow forever.</summary>
    private void PruneBackups()
    {
        try
        {
            var directory = Path.GetDirectoryName(DatabasePath);
            if (directory is null)
            {
                return;
            }

            var stale = Directory
                .GetFiles(directory, $"{DatabaseFileName}-backup-*")
                .OrderByDescending(path => path, StringComparer.Ordinal)
                .Skip(KeepBackupCount)
                .ToList();

            foreach (var path in stale)
            {
                DeleteFileIfExists(path);
            }
        }
        catch (Exception)
        {
            // Housekeeping only; a leftover backup is harmless.
        }
    }

    /// <summary>Deletes "-wal", "-shm" (SQLite) and "-log" (LiteDB) files of the database.</summary>
    private void DeleteSidecarFiles()
    {
        foreach (var suffix in SidecarSuffixes)
        {
            DeleteFileIfExists(DatabasePath + suffix);
        }
    }

    private static void DeleteFileIfExists(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception)
        {
            // Best effort: a locked sidecar file must not abort the operation.
        }
    }
}

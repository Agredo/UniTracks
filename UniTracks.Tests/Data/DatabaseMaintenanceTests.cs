using System.Text;
using UniTracks.Models.Constants;
using UniTracks.Services.Data;
using UniTracks.Tests.ViewModels.Fakes;

namespace UniTracks.Tests.Data;

/// <summary>
/// Tests for <see cref="DatabaseMaintenance"/>. Import and reset are the two operations that can cost
/// the user their whole history, so they are pinned down here on a real (temporary) directory: what is
/// written when, what is refused, and what is kept as a safety copy.
/// </summary>
public sealed class DatabaseMaintenanceTests : IDisposable
{
    private const string SqliteHeader = "SQLite format 3\0";
    private const string LiteDbHeader = "** This is a LiteDB file **";

    private readonly string directory;
    private readonly FakeFileSystem fileSystem = new();

    public DatabaseMaintenanceTests()
    {
        directory = Path.Combine(Path.GetTempPath(), $"unitracks-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        fileSystem.AppDataDirectory = directory;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (Exception)
        {
            // A leftover temp folder must not fail the suite.
        }
    }

    [Fact]
    public void DatabaseFile_MatchesTheStoreOfThePlatform()
    {
        Assert.Equal(ApplicationConstants.SQliteDatabaseName, CreateMaintenance(useLiteDatabase: false).DatabaseFileName);
        Assert.Equal(ApplicationConstants.LiteDBName, CreateMaintenance(useLiteDatabase: true).DatabaseFileName);
    }

    [Fact]
    public void DatabasePath_SitsInTheAppDataDirectory()
    {
        var maintenance = CreateMaintenance(useLiteDatabase: false);

        Assert.Equal(Path.Combine(directory, ApplicationConstants.SQliteDatabaseName), maintenance.DatabasePath);
    }

    [Fact]
    public void Size_IsZeroWithoutADatabaseFile()
    {
        Assert.Equal(0, CreateMaintenance(useLiteDatabase: false).DatabaseSizeBytes);
    }

    [Fact]
    public void Size_ReportsTheFileLength()
    {
        WriteDatabase(CreateMaintenance(useLiteDatabase: false));

        Assert.True(CreateMaintenance(useLiteDatabase: false).DatabaseSizeBytes > 0);
    }

    [Theory]
    [InlineData(SqliteHeader, false, true)]
    [InlineData(LiteDbHeader, false, false)]
    [InlineData(SqliteHeader, true, false)]
    [InlineData(LiteDbHeader, true, true)]
    public void HeaderCheck_OnlyAcceptsTheStoreOfThisPlatform(string header, bool useLiteDatabase, bool expected)
    {
        var path = Path.Combine(directory, "fremd.db");
        WriteText(path, header);

        Assert.Equal(expected, CreateMaintenance(useLiteDatabase).IsValidDatabaseFile(path));
    }

    [Fact]
    public void HeaderCheck_RejectsATruncatedFile()
    {
        var path = Path.Combine(directory, "leer.db");
        WriteText(path, "SQL");

        Assert.False(CreateMaintenance(useLiteDatabase: false).IsValidDatabaseFile(path));
    }

    [Fact]
    public void HeaderCheck_RejectsAMissingFile() =>
        Assert.False(CreateMaintenance(useLiteDatabase: false).IsValidDatabaseFile(Path.Combine(directory, "gibtsnicht.db")));

    [Fact]
    public async Task StageImport_WithoutAPickedFile_ExplainsItself()
    {
        var maintenance = CreateMaintenance(useLiteDatabase: false);

        Assert.NotNull(await maintenance.StageImportAsync(null));
        Assert.NotNull(await maintenance.StageImportAsync("   "));
        Assert.False(maintenance.HasPendingOperation);
    }

    [Fact]
    public async Task StageImport_WithAMissingFile_ExplainsItself()
    {
        var error = await CreateMaintenance(useLiteDatabase: false)
            .StageImportAsync(Path.Combine(directory, "gibtsnicht.db"));

        Assert.Contains("nicht gefunden", error);
    }

    [Fact]
    public async Task StageImport_RefusesAFileOfTheOtherStore()
    {
        var path = Path.Combine(directory, "litedb.db");
        WriteText(path, LiteDbHeader);

        var error = await CreateMaintenance(useLiteDatabase: false).StageImportAsync(path);

        Assert.Contains("keine UniTracks-Datenbank", error);
        Assert.False(CreateMaintenance(useLiteDatabase: false).HasPendingOperation);
    }

    [Fact]
    public async Task StageImport_CopiesTheFileAndWaitsForTheNextStart()
    {
        var source = Path.Combine(directory, "import.db");
        WriteText(source, SqliteHeader + "nutzdaten");
        var maintenance = CreateMaintenance(useLiteDatabase: false);
        WriteDatabase(maintenance);
        WriteText(maintenance.DatabasePath + "-wal", "wal");

        Assert.Null(await maintenance.StageImportAsync(source));

        Assert.True(maintenance.HasPendingOperation);
        Assert.Contains("Import", maintenance.PendingOperationText);
        Assert.True(File.Exists(maintenance.DatabasePath + DatabaseMaintenance.ImportPendingSuffix));
        // The live database is untouched until the app restarts.
        Assert.Contains("nutzdaten", File.ReadAllText(maintenance.DatabasePath));
    }

    [Fact]
    public async Task StageImport_ReplacesAStagedReset()
    {
        var maintenance = CreateMaintenance(useLiteDatabase: false);
        var source = Path.Combine(directory, "import.db");
        WriteText(source, SqliteHeader);
        Assert.Null(await maintenance.StageResetAsync());

        Assert.Null(await maintenance.StageImportAsync(source));

        // The newer decision wins, otherwise the import would be deleted on the next start.
        Assert.False(File.Exists(maintenance.DatabasePath + DatabaseMaintenance.ResetPendingSuffix));
        Assert.Contains("Import", maintenance.PendingOperationText);
    }

    [Fact]
    public async Task StageReset_ReplacesAStagedImport()
    {
        var maintenance = CreateMaintenance(useLiteDatabase: false);
        var source = Path.Combine(directory, "import.db");
        WriteText(source, SqliteHeader);
        Assert.Null(await maintenance.StageImportAsync(source));

        Assert.Null(await maintenance.StageResetAsync());

        Assert.False(File.Exists(maintenance.DatabasePath + DatabaseMaintenance.ImportPendingSuffix));
        Assert.Contains("gelöscht", maintenance.PendingOperationText);
    }

    [Fact]
    public void NoPendingOperation_ReportsNothingToDo()
    {
        var maintenance = CreateMaintenance(useLiteDatabase: false);

        Assert.False(maintenance.HasPendingOperation);
        Assert.Empty(maintenance.PendingOperationText);

        var result = maintenance.ApplyPending();

        Assert.False(result.Applied);
        Assert.Null(result.Message);
    }

    [Fact]
    public async Task ApplyPending_Import_ReplacesTheDatabaseAndKeepsABackup()
    {
        var maintenance = CreateMaintenance(useLiteDatabase: false);
        WriteDatabase(maintenance, "alte daten");
        var source = Path.Combine(directory, "import.db");
        WriteText(source, SqliteHeader + "neue daten");
        await maintenance.StageImportAsync(source);

        var result = maintenance.ApplyPending();

        Assert.True(result.Applied);
        Assert.Equal("Import übernommen.", result.Message);
        Assert.Contains("neue daten", File.ReadAllText(maintenance.DatabasePath));
        Assert.Single(BackupFiles());
        Assert.False(File.Exists(maintenance.DatabasePath + DatabaseMaintenance.ImportPendingSuffix));
        Assert.NotNull(maintenance.LastImport);
    }

    [Fact]
    public async Task ApplyPending_Import_DeletesTheSidecarFilesOfTheOldDatabase()
    {
        var maintenance = CreateMaintenance(useLiteDatabase: false);
        WriteDatabase(maintenance);
        WriteText(maintenance.DatabasePath + "-wal", "wal");
        WriteText(maintenance.DatabasePath + "-shm", "shm");
        var source = Path.Combine(directory, "import.db");
        WriteText(source, SqliteHeader + "neu");
        await maintenance.StageImportAsync(source);

        maintenance.ApplyPending();

        // A leftover WAL would be replayed over the imported file.
        Assert.False(File.Exists(maintenance.DatabasePath + "-wal"));
        Assert.False(File.Exists(maintenance.DatabasePath + "-shm"));
    }

    [Fact]
    public async Task ApplyPending_ImportWithoutAnExistingDatabase_SkipsTheBackup()
    {
        var maintenance = CreateMaintenance(useLiteDatabase: false);
        var source = Path.Combine(directory, "import.db");
        WriteText(source, SqliteHeader + "neu");
        await maintenance.StageImportAsync(source);

        var result = maintenance.ApplyPending();

        Assert.True(result.Applied);
        Assert.Empty(BackupFiles());
    }

    [Fact]
    public async Task ApplyPending_DiscardsATamperedImportFile()
    {
        var maintenance = CreateMaintenance(useLiteDatabase: false);
        WriteDatabase(maintenance, "alte daten");
        var pending = maintenance.DatabasePath + DatabaseMaintenance.ImportPendingSuffix;
        WriteText(pending, "kein datenbankkopf");

        var result = maintenance.ApplyPending();

        Assert.False(result.Applied);
        Assert.Contains("verworfen", result.Message);
        Assert.False(File.Exists(pending));
        Assert.Contains("alte daten", File.ReadAllText(maintenance.DatabasePath));
    }

    [Fact]
    public async Task ApplyPending_Reset_DeletesTheDatabaseButKeepsABackup()
    {
        var maintenance = CreateMaintenance(useLiteDatabase: false);
        WriteDatabase(maintenance, "alte daten");
        await maintenance.StageResetAsync();

        var result = maintenance.ApplyPending();

        Assert.True(result.Applied);
        Assert.Equal("Daten gelöscht.", result.Message);
        Assert.False(File.Exists(maintenance.DatabasePath));
        Assert.False(maintenance.HasPendingOperation);

        var backup = Assert.Single(BackupFiles());
        Assert.Contains("alte daten", File.ReadAllText(backup));
        Assert.Null(maintenance.LastImport);
    }

    [Fact]
    public async Task ApplyPending_ResetWithoutADatabase_StillSucceeds()
    {
        var maintenance = CreateMaintenance(useLiteDatabase: false);
        await maintenance.StageResetAsync();

        var result = maintenance.ApplyPending();

        Assert.True(result.Applied);
        Assert.Empty(BackupFiles());
    }

    [Fact]
    public async Task CreateExportCopy_WithoutADatabase_ReturnsNothing() =>
        Assert.Null(await CreateMaintenance(useLiteDatabase: false).CreateExportCopyAsync());

    [Fact]
    public async Task CreateExportCopy_LeavesTheLiveFileAlone()
    {
        var maintenance = CreateMaintenance(useLiteDatabase: false);
        WriteDatabase(maintenance, "alte daten");

        var copy = await maintenance.CreateExportCopyAsync();

        Assert.NotNull(copy);
        Assert.NotEqual(maintenance.DatabasePath, copy);
        Assert.StartsWith($"UniTracks-{DateTime.Now:yyyy-MM-dd}", Path.GetFileName(copy));
        Assert.EndsWith(".db", copy);
        Assert.Contains("alte daten", File.ReadAllText(copy!));
        // The copy lives next to the original, so the share sheet never sees the open file.
        Assert.Equal(directory, Path.GetDirectoryName(copy));
    }

    [Fact]
    public async Task CreateExportCopy_KeepsTheTwoNewestCopies()
    {
        var maintenance = CreateMaintenance(useLiteDatabase: false);
        WriteDatabase(maintenance);
        for (var index = 0; index < 4; index++)
        {
            WriteText(Path.Combine(directory, $"UniTracks-2020-01-0{index + 1}-0800.db"), "alt");
        }

        await maintenance.CreateExportCopyAsync();

        var exports = Directory.GetFiles(directory, "UniTracks-*");
        Assert.Equal(2, exports.Length);
    }

    [Fact]
    public async Task LastImport_IsReadFromTheRecordFile()
    {
        var maintenance = CreateMaintenance(useLiteDatabase: false);
        WriteDatabase(maintenance);
        var source = Path.Combine(directory, "import.db");
        WriteText(source, SqliteHeader + "neu");
        await maintenance.StageImportAsync(source);
        maintenance.ApplyPending();

        var lastImport = maintenance.LastImport;

        Assert.NotNull(lastImport);
        Assert.True(DateTimeOffset.Now - lastImport.Value < TimeSpan.FromMinutes(1));
    }

    private DatabaseMaintenance CreateMaintenance(bool useLiteDatabase) => new(fileSystem, useLiteDatabase);

    private void WriteDatabase(DatabaseMaintenance maintenance, string payload = "nutzdaten") =>
        WriteText(maintenance.DatabasePath, SqliteHeader + payload);

    private string[] BackupFiles()
    {
        var maintenance = CreateMaintenance(useLiteDatabase: false);
        return Directory.GetFiles(directory, $"{maintenance.DatabaseFileName}-backup-*");
    }

    private static void WriteText(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, Encoding.ASCII.GetBytes(content));
    }
}

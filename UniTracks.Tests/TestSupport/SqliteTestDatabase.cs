using Microsoft.EntityFrameworkCore;
using UniTracks.Data.Repository;
using UniTracks.Data.SQLite;

namespace UniTracks.Tests.TestSupport;

/// <summary>
/// A throwaway SQLite database on disk, wired up exactly like the app does it.
/// <para>
/// The options-based <see cref="SqliteDBContext"/> constructor is used on purpose: it is the only
/// one that does not run migrations implicitly, so the schema is created explicitly here. The
/// connection string disables pooling so the file handle is released on dispose and the temp file
/// can actually be deleted again.
/// </para>
/// </summary>
internal sealed class SqliteTestDatabase : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"unitracks-tests-{Guid.NewGuid():N}.db");

    public SqliteTestDatabase(Action<DbContextOptionsBuilder<SqliteDBContext>>? configure = null)
    {
        var builder = new DbContextOptionsBuilder<SqliteDBContext>()
            .UseSqlite($"Filename={_path};Pooling=False");

        configure?.Invoke(builder);

        Context = new SqliteDBContext(builder.Options);
        Context.Database.Migrate();
        Repository = new EfRepository(Context);
    }

    public SqliteDBContext Context { get; }

    public EfRepository Repository { get; }

    public void Dispose()
    {
        Context.Dispose();

        // SQLite may have left a write-ahead log behind; clean up every sidecar file.
        foreach (var file in new[] { _path, _path + "-wal", _path + "-shm" })
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch (IOException)
            {
                // A leftover temp file must never fail a test run.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}

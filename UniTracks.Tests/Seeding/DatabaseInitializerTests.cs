using UniTracks.Data.LiteDB;
using UniTracks.Data.Repository;
using UniTracks.Data.Seeding;
using UniTracks.Models.Trip;
using UniTracks.Tests.TestSupport;

namespace UniTracks.Tests.Seeding;

/// <summary>
/// <see cref="DatabaseInitializer"/> is called from <c>App</c>'s constructor, which blocks the UI
/// thread on the result. That only works as long as the method never posts a continuation back to
/// the calling thread — which is exactly what broke the iOS app: once the store answered
/// asynchronously, the continuation was queued to the blocked UI thread, the launch never finished
/// and iOS' launch watchdog killed the app.
/// </summary>
public sealed class DatabaseInitializerTests
{
    /// <summary>
    /// A synchronization context that never runs what is posted to it — the state of a UI thread that
    /// is blocked in <c>GetAwaiter().GetResult()</c>. Anything that captures this context deadlocks.
    /// </summary>
    private sealed class BlockedSynchronizationContext : SynchronizationContext
    {
        public int PostedCount;

        public override void Post(SendOrPostCallback d, object? state)
            => Interlocked.Increment(ref PostedCount);
    }

    /// <summary>
    /// Runs <paramref name="blockingCall"/> on a thread that carries a synchronization context and
    /// reports whether it returned within the timeout, the way the app's launch would be killed.
    /// </summary>
    private static (bool Completed, int PostedCount) RunWithBlockedContext(Action blockingCall)
    {
        var context = new BlockedSynchronizationContext();
        var completed = false;

        var thread = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(context);
            blockingCall();
            completed = true;
        })
        {
            IsBackground = true,
        };

        thread.Start();

        if (!thread.Join(TimeSpan.FromSeconds(10)))
        {
            return (false, context.PostedCount);
        }

        return (completed, context.PostedCount);
    }

    [Fact]
    public void EnsureSeededAsync_ReturnsWhileTheCallersContextIsBlocked()
    {
        var path = Path.Combine(Path.GetTempPath(), $"unitracks-seed-{Guid.NewGuid():N}.litedb");
        var database = new LiteDatabase(path);

        try
        {
            var repository = new LiteDbRepository(database);
            var initializer = new DatabaseInitializer(repository);

            // Blocking on purpose: this is the exact call App's constructor makes.
#pragma warning disable xUnit1031
            var (completed, postedCount) = RunWithBlockedContext(
                () => initializer.EnsureSeededAsync().GetAwaiter().GetResult());
#pragma warning restore xUnit1031

            Assert.True(
                completed,
                "EnsureSeededAsync did not return: it captured the caller's synchronization context, "
                + "which is what made the iOS app fail to finish its launch.");

            // A continuation must never be handed to the calling context - that context is the UI thread.
            Assert.Equal(0, postedCount);
        }
        finally
        {
            database.Database.Dispose();
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task EnsureSeededAsync_FillsAnEmptyStoreAndThenStaysIdle()
    {
        var path = Path.Combine(Path.GetTempPath(), $"unitracks-seed-{Guid.NewGuid():N}.litedb");
        var database = new LiteDatabase(path);

        try
        {
            var repository = new LiteDbRepository(database);
            var initializer = new DatabaseInitializer(repository);

            await initializer.EnsureSeededAsync();

            var seeded = (await repository.GetAllAsync<TripType>()).ToList();
            Assert.NotEmpty(seeded);

            // Second call is a no-op, so the catalog is not duplicated.
            await initializer.EnsureSeededAsync();
            Assert.Equal(seeded.Count, (await repository.GetAllAsync<TripType>()).Count());
        }
        finally
        {
            database.Database.Dispose();
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task EnsureSeededAsync_CarriesRenamedCatalogEntriesIntoAnOlderStore()
    {
        // A store that was seeded before the catalog was translated: the same id, the old English name.
        var repository = new InMemoryRepository();
        repository.Seed(new TripType
        {
            ID = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Identifier = "run",
            Name = "Run",
            Description = string.Empty,
            Category = "running",
        });

        await new DatabaseInitializer(repository).EnsureSeededAsync();

        var types = (await repository.GetAllAsync<TripType>()).ToList();

        // The stale row is updated, not duplicated, and nothing else about it changes.
        var run = Assert.Single(types, type => type.Identifier == "run");
        Assert.Equal("Laufen", run.Name);
        Assert.Equal("running", run.Category);
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000001"), run.ID);

        // The remaining catalog entries are still added.
        Assert.Equal(TripTypeSeeds.Load().Count, types.Count);
    }

    [Fact]
    public async Task EnsureSeededAsync_LeavesAnAlreadyCurrentCatalogUntouched()
    {
        var repository = new InMemoryRepository();
        var initializer = new DatabaseInitializer(repository);

        await initializer.EnsureSeededAsync();
        var versionAfterSeeding = repository.DataVersion;

        // A fresh initializer, as a later app start would create it.
        await new DatabaseInitializer(repository).EnsureSeededAsync();

        Assert.Equal(versionAfterSeeding, repository.DataVersion);
        Assert.Equal(TripTypeSeeds.Load().Count, (await repository.GetAllAsync<TripType>()).Count());
    }
}

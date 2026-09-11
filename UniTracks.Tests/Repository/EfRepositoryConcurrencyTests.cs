using System.Collections.Concurrent;
using UniTracks.Games.TowerDefense.Persistence;
using UniTracks.Tests.TestSupport;

namespace UniTracks.Tests.Repository;

/// <summary>
/// Regression tests for finding #1: <c>EfRepository</c> is a singleton reached concurrently by the
/// UI thread, the fire-and-forget startup distance recalculation and the platform location
/// callback. Before the semaphore gate this surfaced as
/// "A second operation was started on this context instance before a previous operation completed".
/// <para>
/// The operations are driven from raw threads that meet at a barrier instead of from
/// <see cref="Task.WhenAll"/>: SQLite completes its async APIs synchronously, so a pile of queued
/// tasks runs strictly one after another and never overlaps, which would make these tests pass even
/// against the broken implementation.
/// </para>
/// </summary>
public sealed class EfRepositoryConcurrencyTests
{
    private const int WorkerCount = 32;
    private static readonly TimeSpan WorkerTimeout = TimeSpan.FromSeconds(30);

    private static TowerUnlock Unlock(int index) => new()
    {
        ID = Guid.NewGuid(),
        TowerId = $"tower-{index}",
    };

    /// <summary>
    /// Runs <paramref name="work"/> on <paramref name="workers"/> threads that all enter the
    /// repository at the same instant.
    /// </summary>
    private static async Task<(List<Exception> Errors, bool AllFinished)> RunInParallelAsync(
        int workers,
        Action<int> work)
    {
        using var barrier = new Barrier(workers);
        var errors = new ConcurrentBag<Exception>();

        var threads = Enumerable.Range(0, workers)
            .Select(index => new Thread(() =>
            {
                try
                {
                    barrier.SignalAndWait();
                    work(index);
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            })
            {
                IsBackground = true,
            })
            .ToList();

        foreach (var thread in threads)
        {
            thread.Start();
        }

        var joined = await Task.Run(
            () => threads.Select(thread => thread.Join(WorkerTimeout)).ToList(),
            TestContext.Current.CancellationToken);

        return (errors.ToList(), joined.All(joinedThread => joinedThread));
    }

    [Fact]
    public async Task ConcurrentAdds_AllPersistWithoutContextReuseFailures()
    {
        using var db = new SqliteTestDatabase();
        var repository = db.Repository;

        var (errors, allFinished) = await RunInParallelAsync(
            WorkerCount,
            index => repository.Add(Unlock(index)).GetAwaiter().GetResult());

        Assert.True(allFinished, "Mindestens ein Thread kehrte nicht zurück (Deadlock?).");
        Assert.Empty(errors);

        var stored = (await repository.GetAllAsync<TowerUnlock>()).ToList();
        Assert.Equal(WorkerCount, stored.Count);
        Assert.Equal(WorkerCount, stored.Select(unlock => unlock.TowerId).Distinct().Count());
    }

    [Fact]
    public async Task MixedConcurrentReadsAndWrites_AreSerialized()
    {
        using var db = new SqliteTestDatabase();
        var repository = db.Repository;

        var seeded = new List<TowerUnlock>();
        for (var index = 0; index < 40; index++)
        {
            seeded.Add(await repository.Add(Unlock(index)));
        }

        // Deleted rows and updated rows are disjoint: overlapping them would be a race in the test,
        // not in the repository (an UPDATE against a deleted row legitimately affects 0 rows).
        var toDelete = seeded.Take(5).ToList();
        var toUpdate = seeded.Skip(10).ToList();

        var (errors, allFinished) = await RunInParallelAsync(30, index =>
        {
            if (index < 10)
            {
                repository.Add(Unlock(1000 + index)).GetAwaiter().GetResult();
            }
            else if (index < 20)
            {
                repository.Update(toUpdate[index - 10]).GetAwaiter().GetResult();
            }
            else if (index < 25)
            {
                repository.DeleteRange(new[] { toDelete[index - 20] }).GetAwaiter().GetResult();
            }
            else
            {
                _ = repository.Get<TowerUnlock>().ToList();
            }
        });

        Assert.True(allFinished, "Mindestens ein Thread kehrte nicht zurück (Deadlock?).");
        Assert.Empty(errors);

        var remaining = (await repository.GetAllAsync<TowerUnlock>()).ToList();
        Assert.Equal(45, remaining.Count);
        Assert.DoesNotContain(remaining, unlock => toDelete.Any(deleted => deleted.ID == unlock.ID));
    }

    [Fact]
    public async Task ConcurrentSyncAndAsyncReads_AllReturnTheSameResult()
    {
        using var db = new SqliteTestDatabase();
        var repository = db.Repository;

        await repository.Add(Unlock(1));

        var (errors, allFinished) = await RunInParallelAsync(16, index =>
        {
            var rows = index % 2 == 0
                ? repository.Get<TowerUnlock>()
                : repository.GetAllAsync<TowerUnlock>().GetAwaiter().GetResult();

            Assert.Single(rows);
        });

        Assert.True(allFinished, "Mindestens ein Thread kehrte nicht zurück (Deadlock?).");
        Assert.Empty(errors);
    }

    /// <summary>
    /// The deadlock trap behind finding #1: <c>Get&lt;T&gt;()</c> has a synchronous signature and
    /// blocks the calling thread on the gate, while the other members await it. If those awaits did
    /// not use <c>ConfigureAwait(false)</c>, the continuation that releases the gate would be posted
    /// back to the blocked thread and the gate would never be released.
    /// </summary>
    [Fact]
    public async Task SynchronousGet_DoesNotDeadlockAgainstAnInFlightOperation()
    {
        var interceptor = new SuspendingCommandInterceptor("TowerUnlock");
        using var db = new SqliteTestDatabase(builder => builder.AddInterceptors(interceptor));
        var repository = db.Repository;

        List<TowerUnlock>? rows = null;
        Exception? failure = null;
        var completed = false;

        var worker = new Thread(() =>
        {
            var original = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(new NonPumpingSynchronizationContext());
            try
            {
                var inFlight = repository.GetAllAsync<TowerUnlock>();

                // Blocks on the gate until the parked operation above releases it - and the thread
                // that would have to run its continuation is this very thread.
                rows = repository.Get<TowerUnlock>().ToList();
                inFlight.GetAwaiter().GetResult();
                completed = true;
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(original);
            }
        })
        {
            IsBackground = true,
        };

        worker.Start();

        // Give the worker time to reach gate.Wait() before letting the parked read finish.
        var resumer = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(250)).ConfigureAwait(false);
            interceptor.Resume();
        }, TestContext.Current.CancellationToken);

        var joined = await Task.Run(
            () => worker.Join(WorkerTimeout),
            TestContext.Current.CancellationToken);

        await resumer;

        Assert.True(joined, "Get<T>() blockierte dauerhaft: die Fortsetzung wurde an den blockierten Thread zurückgepostet.");
        Assert.Null(failure);
        Assert.True(completed);
        Assert.True(interceptor.WasSuspended, "Der Test hat die Operation nicht wirklich angehalten.");
        Assert.Empty(rows!);
    }

    /// <summary>
    /// Mimics a UI thread that is blocked and therefore never pumps its message queue. Anything
    /// posted here stalls forever, so a correct repository has to reach <c>gate.Release()</c>
    /// without routing a continuation back through this context.
    /// </summary>
    private sealed class NonPumpingSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
        {
            // Intentionally never invokes the callback.
        }

        public override void Send(SendOrPostCallback d, object? state) => d(state);
    }
}

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace UniTracks.Tests.TestSupport;

/// <summary>
/// Holds the first <c>SELECT</c> against <paramref name="tableName"/> open until <see cref="Resume"/>
/// is called, so a test can reliably park a repository call *while it is suspended at an await*.
/// <para>
/// This is needed because SQLite executes synchronously under the hood: without an artificial
/// suspension the async repository members never actually yield, and a "concurrent" test would
/// quietly run one call after another without ever overlapping.
/// </para>
/// </summary>
internal sealed class SuspendingCommandInterceptor(string tableName) : DbCommandInterceptor
{
    private readonly string _marker = $"FROM \"{tableName}";
    private readonly ManualResetEventSlim _resume = new(false);
    private int _suspended;

    /// <summary>Whether a command was actually parked, i.e. the test exercised the intended path.</summary>
    public bool WasSuspended => Volatile.Read(ref _suspended) == 1;

    public void Resume() => _resume.Set();

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        // Only the first matching read is parked; DDL from Database.Migrate() never matches.
        if (command.CommandText.Contains(_marker, StringComparison.OrdinalIgnoreCase)
            && Interlocked.Exchange(ref _suspended, 1) == 0)
        {
            // ConfigureAwait(false) is deliberate here: the interceptor must not be what decides
            // whether the repository's continuations return to the captured context, otherwise the
            // test could not tell the two implementations apart.
            await Task.Run(() => _resume.Wait(TimeSpan.FromSeconds(20)), cancellationToken)
                .ConfigureAwait(false);
        }

        return result;
    }
}

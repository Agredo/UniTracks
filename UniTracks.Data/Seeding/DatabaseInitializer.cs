using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UniTracks.Data.Repository;
using UniTracks.Models.Trip;

namespace UniTracks.Data.Seeding;

/// <summary>
/// Seeds the active repository with the TripType catalog when it is empty. This is required on
/// iOS where the store is LiteDB and there is no EF Core HasData/migration seeding; on Android,
/// Mac Catalyst and Windows the repository is already seeded by EF migrations, so this is a no-op.
/// </summary>
public class DatabaseInitializer
{
    private readonly IRepository _repository;
    private bool _seeded;

    public DatabaseInitializer(IRepository repository)
    {
        _repository = repository;
    }

    public async Task EnsureSeededAsync()
    {
        if (_seeded)
            return;

        try
        {
            // ConfigureAwait(false) is required here: App calls this from its constructor and blocks
            // the UI thread on the result. Without it the continuation would be posted back to the
            // (blocked) UI thread as soon as the store answers asynchronously, and iOS' launch
            // watchdog would kill the app for an unfinished launch.
            if (!(await _repository.GetAllAsync<TripType>().ConfigureAwait(false)).Any())
            {
                foreach (var tripType in TripTypeSeeds.Load())
                {
                    await _repository.Add(tripType).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            // Best-effort seeding: a failure must never prevent app startup.
            Debug.WriteLine($"DatabaseInitializer: TripType seeding failed: {ex}");
            _seeded = false;
            return;
        }

        _seeded = true;
    }
}

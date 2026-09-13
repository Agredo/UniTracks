using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UniTracks.Data.Repository;
using UniTracks.Models.Trip;

namespace UniTracks.Data.Seeding;

/// <summary>
/// Seeds the active repository with the TripType catalog. This is required on iOS where the store is
/// LiteDB and there is no EF Core HasData/migration seeding; on Android, Mac Catalyst and Windows the
/// repository is already seeded by EF migrations, so the seeding part is a no-op there. Renamed
/// catalog entries are carried over on every platform, so a store seeded by an older version does not
/// keep showing the old names.
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
            var existing = (await _repository.GetAllAsync<TripType>().ConfigureAwait(false))
                .ToDictionary(tripType => tripType.ID);

            foreach (var seed in TripTypeSeeds.Load())
            {
                if (!existing.TryGetValue(seed.ID, out var stored))
                {
                    await _repository.Add(seed).ConfigureAwait(false);
                    continue;
                }

                // The catalog ships with the app, so a name changed in a newer version has to be
                // written to the stored row as well. Identifiers and categories stay untouched, so
                // trip matching and the game economy are unaffected.
                if (!string.Equals(stored.Name, seed.Name, StringComparison.Ordinal))
                {
                    await _repository.Update(stored with { Name = seed.Name }).ConfigureAwait(false);
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

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
            if (!(await _repository.GetAllAsync<TripType>()).Any())
            {
                foreach (var tripType in TripTypeSeeds.Load())
                {
                    await _repository.Add(tripType);
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

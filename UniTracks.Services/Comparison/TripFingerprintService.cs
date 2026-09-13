using UniTracks.Data.Repository;
using UniTracks.Models.Comparison;
using UniTracks.Models.Trip;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.Services.Comparison;

/// <summary>
/// Owns the stored trip fingerprints: reads them, rebuilds the missing or outdated ones, and drops
/// the one belonging to a deleted trip.
/// </summary>
public interface ITripFingerprintService
{
    /// <summary>All stored fingerprints. Cheap enough to load whole — they carry no GPS points.</summary>
    Task<IReadOnlyList<TripFingerprint>> GetAllAsync();

    Task<TripFingerprint?> GetAsync(Guid tripId);

    /// <summary>
    /// Returns the trip's fingerprint, deriving and storing it when it is missing or was built by an
    /// older <see cref="TripFingerprintBuilder.Version"/>.
    /// </summary>
    Task<TripFingerprint?> EnsureAsync(Trip trip);

    /// <summary>
    /// Derives fingerprints for every trip that does not have a current one. This is the only part of
    /// the feature that has to touch GPS points in bulk, so it runs once after an update and never on
    /// the path of an ordinary comparison.
    /// </summary>
    /// <returns>How many fingerprints were created or rebuilt.</returns>
    Task<int> BackfillAsync(CancellationToken cancellationToken = default);

    /// <summary>Drops a trip's fingerprint; called when the trip itself is deleted.</summary>
    Task RemoveAsync(Guid tripId);
}

/// <inheritdoc />
public sealed class TripFingerprintService : ITripFingerprintService
{
    /// <summary>
    /// Trips per backfill round. Loading a trip's GPS points is the expensive part, so the work is
    /// chunked to keep peak memory bounded on a phone holding years of tracks.
    /// </summary>
    private const int BackfillChunkSize = 20;

    private readonly IRepository repository;
    private readonly ITripTypeCatalog tripTypes;

    public TripFingerprintService(IRepository repository, ITripTypeCatalog tripTypes)
    {
        this.repository = repository;
        this.tripTypes = tripTypes;
    }

    public async Task<IReadOnlyList<TripFingerprint>> GetAllAsync() =>
        (await LoadStoredAsync())
            .GroupBy(fingerprint => fingerprint.TripID)
            .Select(NewestFirst)
            .Select(rows => rows[0])
            .ToList();

    public Task<TripFingerprint?> GetAsync(Guid tripId) => repository.GetByIdAsync<TripFingerprint>(tripId);

    public async Task<TripFingerprint?> EnsureAsync(Trip trip)
    {
        var existing = await repository.GetByIdAsync<TripFingerprint>(trip.ID);
        if (existing is not null && !TripFingerprintBuilder.IsStale(existing))
        {
            return existing;
        }

        var type = await tripTypes.GetAsync(trip.TripTypeId);
        var built = TripFingerprintBuilder.Build(await WithLocationsAsync(trip), type);
        if (built is null)
        {
            return existing;
        }

        return existing is null
            ? await repository.Add(built)
            : await repository.Update(built);
    }

    public async Task RemoveAsync(Guid tripId)
    {
        var existing = await repository.GetByIdAsync<TripFingerprint>(tripId);
        if (existing is not null)
        {
            await repository.Delete(existing);
        }
    }

    public async Task<int> BackfillAsync(CancellationToken cancellationToken = default)
    {
        // Grouped instead of ToDictionary: a store that wrote several rows for one trip used to blow
        // the backfill up with "an item with the same key has already been added" and left the
        // library without any index at all.
        var stored = new Dictionary<Guid, TripFingerprint>();

        foreach (var group in (await LoadStoredAsync()).GroupBy(fingerprint => fingerprint.TripID))
        {
            var rows = NewestFirst(group);
            stored[rows[0].TripID] = rows[0];

            // Self-heal. Older builds stored the fingerprint without a document key, so every index
            // pass added another row for the same trip - visible as doubled entries everywhere. The
            // surplus is dropped here, which is the one moment the whole library is in hand anyway.
            for (int index = 1; index < rows.Count; index++)
            {
                await repository.Delete(rows[index]);
            }
        }

        var trips = (await repository.GetAllAsync<Trip>()).ToList();
        var pending = trips
            .Where(trip => !stored.TryGetValue(trip.ID, out var fingerprint) || TripFingerprintBuilder.IsStale(fingerprint))
            .ToList();

        // Read once: the catalogue is 85 static rows, and every rebuilt fingerprint needs its type.
        var types = await tripTypes.GetAllAsync();

        int rebuilt = 0;

        for (int offset = 0; offset < pending.Count; offset += BackfillChunkSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chunk = pending.GetRange(offset, Math.Min(BackfillChunkSize, pending.Count - offset));
            var locationsByTrip = await LoadLocationsAsync(chunk, cancellationToken);

            foreach (var trip in chunk)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (locationsByTrip.TryGetValue(trip.ID, out var locations))
                {
                    trip.Locations = locations;
                }

                var type = trip.TripTypeId is Guid id && types.TryGetValue(id, out var known) ? known : null;
                var built = TripFingerprintBuilder.Build(trip, type);
                if (built is null)
                {
                    continue;
                }

                if (stored.ContainsKey(trip.ID))
                {
                    await repository.Update(built);
                }
                else
                {
                    await repository.Add(built);
                }

                rebuilt++;
            }
        }

        return rebuilt;
    }

    /// <summary>
    /// Fetches the GPS points for a batch of trips in one query per chunk. On stores with embedded
    /// documents the points are already attached to the trip and no read is needed at all.
    /// </summary>
    private async Task<Dictionary<Guid, List<LocationModel>>> LoadLocationsAsync(
        List<Trip> trips,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, List<LocationModel>>();

        var missing = trips.Where(trip => trip.Locations is not { Count: > 0 }).ToList();
        foreach (var trip in trips.Except(missing))
        {
            result[trip.ID] = trip.Locations;
        }

        if (missing.Count == 0)
        {
            return result;
        }

        var ids = missing.Select(trip => trip.ID).ToList();
        var locations = await repository.GetAsync<LocationModel>(location => location.TripID != null && ids.Contains(location.TripID.Value));

        var grouped = locations
            .GroupBy(location => location.TripID!.Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(location => location.Timestamp).ToList());

        foreach (var trip in missing)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (grouped.TryGetValue(trip.ID, out var list))
            {
                result[trip.ID] = list;
            }
        }

        return result;
    }

    /// <summary>
    /// Every stored fingerprint as it sits in the store, duplicates included. Only the backfill wants
    /// them; everything else goes through <see cref="GetAllAsync"/>.
    /// </summary>
    private async Task<List<TripFingerprint>> LoadStoredAsync() =>
        (await repository.GetAllAsync<TripFingerprint>()).ToList();

    /// <summary>Rows of one trip, most recent derivation first.</summary>
    private static List<TripFingerprint> NewestFirst(IEnumerable<TripFingerprint> rows) =>
        rows.OrderByDescending(fingerprint => fingerprint.Version)
            .ThenByDescending(fingerprint => fingerprint.StartTime)
            .ToList();

    private async Task<Trip> WithLocationsAsync(Trip trip)
    {
        if (trip.Locations is { Count: > 0 })
        {
            return trip;
        }

        trip.Locations = (await repository.GetAsync<LocationModel>(location => location.TripID == trip.ID))
            .OrderBy(location => location.Timestamp)
            .ToList();

        return trip;
    }
}

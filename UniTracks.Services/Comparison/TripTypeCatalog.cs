using UniTracks.Data.Repository;
using UniTracks.Models.Trip;

namespace UniTracks.Services.Comparison;

/// <summary>
/// Resolves trip types, because the comparison rules work on the type's category and identifier and
/// a fingerprint must not depend on a navigation being loaded.
/// </summary>
public interface ITripTypeCatalog
{
    /// <summary>All known trip types, keyed by id. Small and static, so it is read whole.</summary>
    Task<IReadOnlyDictionary<Guid, TripType>> GetAllAsync();

    /// <summary>The type with the given id, or null when it is unknown or missing.</summary>
    Task<TripType?> GetAsync(Guid? tripTypeId);
}

/// <inheritdoc />
public sealed class TripTypeCatalog : ITripTypeCatalog
{
    private readonly IRepository repository;

    private IReadOnlyDictionary<Guid, TripType>? cache;
    private long cachedVersion = -1;

    public TripTypeCatalog(IRepository repository)
    {
        this.repository = repository;
    }

    public async Task<IReadOnlyDictionary<Guid, TripType>> GetAllAsync()
    {
        // The catalogue only changes when the app seeds or the user edits types, both of which bump
        // DataVersion, so a version check is enough to stay current.
        if (cache is not null && cachedVersion == repository.DataVersion)
        {
            return cache;
        }

        var types = await repository.GetAllAsync<TripType>();

        cache = types.ToDictionary(type => type.ID);
        cachedVersion = repository.DataVersion;

        return cache;
    }

    public async Task<TripType?> GetAsync(Guid? tripTypeId)
    {
        if (tripTypeId is null)
        {
            return null;
        }

        var all = await GetAllAsync();
        return all.TryGetValue(tripTypeId.Value, out var type) ? type : null;
    }
}

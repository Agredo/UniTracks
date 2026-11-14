using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using LiteDB;
using UniTracks.Data.LiteDB;
using UniTracks.Models.Location;
using UniTracks.Models.Trip;
using ILiteDatabase = UniTracks.Data.LiteDB.ILiteDatabase;

namespace UniTracks.Data.Repository;

/// <summary>
/// LiteDB-backed repository used on iOS (CoreCLR + ReadyToRun, IsDynamicCodeSupported=false),
/// where EF Core cannot run Database.Migrate() or build its model at runtime.
/// <para>
/// Navigations are stored as embedded documents inside the parent, so an include is only honoured for
/// the trip's <c>Locations</c>: those live in their own collection (see <see cref="TripPointStore"/>)
/// and are attached when a caller asks for them. Every other include stays a no-op, because those
/// values are already inside the document.
/// </para>
/// </summary>
public class LiteDbRepository : IRepository
{
    private readonly ILiteDatabase _liteDatabase;
    private readonly TripPointStore _tripPoints;

    // Read-through cache for whole-table reads: the statistics page, the games tab, the user page and
    // the trip list each used to run the same collection scan on every visit. A cached read knows
    // which entity types it was derived from and is dropped as soon as one of them is written, so
    // storing a GPS point no longer throws away the cached trip list. The include set is part of the
    // key, because it decides whether a trip arrives with or without its points.
    private readonly ConcurrentDictionary<(Type Entity, string Includes), CacheEntry> fullReadCache = new();

    // Write counter per entity type - the granularity the cache above needs. The global DataVersion
    // is still bumped on every write, because consumers outside the repository use it as a
    // "something changed" stamp for their own caches.
    private readonly ConcurrentDictionary<Type, long> writeVersions = new();

    // Serialises the scans themselves, so two cold readers do not run the same expensive scan
    // in parallel. Only misses take this lock, cache hits never block.
    private readonly object scanGate = new();

    public LiteDbRepository(ILiteDatabase liteDatabase)
    {
        _liteDatabase = liteDatabase;
        _tripPoints = new TripPointStore(liteDatabase);
        DatabasePath = liteDatabase.DatabasePath;
    }

    public string DatabasePath { get; }

    private long dataVersion;

    /// <inheritdoc />
    public long DataVersion => Interlocked.Read(ref dataVersion);

    /// <summary>The number of writes the given entity type has seen; 0 when it was never written.</summary>
    private long VersionOf(Type type) => writeVersions.TryGetValue(type, out var version) ? version : 0;

    public Task<TEntity> Add<TEntity>(TEntity entity) where TEntity : class
    {
        if (entity is Trip trip)
        {
            _liteDatabase.Database.GetCollection<Trip>().Insert(_tripPoints.PrepareForInsert(trip));
        }
        else
        {
            _liteDatabase.Database.GetCollection<TEntity>().Insert(entity);
        }

        MarkWritten(typeof(TEntity));
        return Task.FromResult(entity);
    }

    public Task<TEntity> Update<TEntity>(TEntity entity) where TEntity : class
    {
        if (entity is Trip trip)
        {
            _liteDatabase.Database.GetCollection<Trip>().Update(_tripPoints.PrepareForUpdate(trip));
        }
        else
        {
            _liteDatabase.Database.GetCollection<TEntity>().Update(entity);
        }

        MarkWritten(typeof(TEntity));
        return Task.FromResult(entity);
    }

    public Task Delete<TEntity>(TEntity entity) where TEntity : class
    {
        DeleteCore(entity);
        MarkWritten(typeof(TEntity));
        return Task.CompletedTask;
    }

    public Task DeleteRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
    {
        foreach (var entity in entities)
        {
            DeleteCore(entity);
        }

        MarkWritten(typeof(TEntity));
        return Task.CompletedTask;
    }

    private void DeleteCore<TEntity>(TEntity entity) where TEntity : class
    {
        // The key is either the conventional "ID" property or the one the model declares with
        // [Key] - TripFingerprint uses "TripID", and requiring the literal name here made deleting
        // a trip's fingerprint throw on iOS.
        var property = typeof(TEntity).GetProperty("ID")
            ?? typeof(TEntity).GetProperties().FirstOrDefault(member => member.GetCustomAttribute<KeyAttribute>() is not null)
            ?? throw new InvalidOperationException($"Entity {typeof(TEntity).Name} has no ID property.");

        var id = property.GetValue(entity)
            ?? throw new InvalidOperationException($"Entity {typeof(TEntity).Name} has no ID value.");

        _liteDatabase.Database.GetCollection<TEntity>().Delete(new BsonValue(id));

        // A trip's points live in their own collection, so deleting the document no longer takes them
        // with it.
        if (entity is Trip trip)
        {
            _tripPoints.Remove(trip.ID);
        }
    }

    /// <summary>
    /// Reads one entity by key. A trip comes back without its GPS points - like the EF repository,
    /// whose <c>FindAsync</c> loads no navigations. Callers that draw a track read the points
    /// explicitly (see <c>TripOverviewViewModel.LoadTrackAsync</c>).
    /// </summary>
    public async Task<TEntity?> GetByIdAsync<TEntity>(Guid id) where TEntity : class
    {
        var collection = _liteDatabase.Database.GetCollection<TEntity>();

        // Off the calling thread: LiteDB reads are synchronous and the callers run on the UI thread.
        return await Task.Run(() => collection.FindById(id)).ConfigureAwait(false);
    }

    public Task<IEnumerable<TEntity>> GetAllAsync<TEntity>(params Expression<Func<TEntity, object>>[] includes) where TEntity : class
    {
        var key = CacheKey<TEntity>(includes);

        // A warm cache is answered on the caller's thread: the documents are already materialised,
        // so there is nothing left to move off the UI thread - and on device the thread hop itself
        // added a visible delay to every page switch.
        if (TryGetCached<TEntity>(key, out var cached))
        {
            return Task.FromResult<IEnumerable<TEntity>>(cached);
        }

        // The read (and the BsonMapper pass that materialises the entities) must happen on the
        // worker thread, not on the caller's. LiteDB reads are synchronous, so returning a lazy
        // ReadOnlyCollection here would run the whole scan on the UI thread - either inside this
        // call or at the first enumeration. Both freeze the UI on every page switch, because
        // FindAll() is the entry point of every view model that loads data.
        return Task.Run(() => (IEnumerable<TEntity>)ReadAllCached<TEntity>(key, includes));
    }

    public Task<IEnumerable<TEntity>> GetAsync<TEntity>(Expression<Func<TEntity, bool>>? filter = null, params Expression<Func<TEntity, object>>[] includes) where TEntity : class
    {
        var key = CacheKey<TEntity>(includes);

        if (filter is null)
        {
            // Same result as GetAllAsync, so it shares its cache entry instead of scanning again.
            if (TryGetCached<TEntity>(key, out var cached))
            {
                return Task.FromResult<IEnumerable<TEntity>>(cached);
            }

            return Task.Run(() => (IEnumerable<TEntity>)ReadAllCached<TEntity>(key, includes));
        }

        // Same reason as GetAllAsync: the query runs synchronously, so it has to leave the caller's
        // thread. Every filter is evaluated by the document store, which means a full collection scan.
        return Task.Run(() =>
        {
            var rows = _liteDatabase.Database.GetCollection<TEntity>().Query().Where(filter).ToList();
            AttachPoints(rows, includes);
            return (IEnumerable<TEntity>)rows;
        });
    }

    public IEnumerable<TEntity> Get<TEntity>(Expression<Func<TEntity, bool>>? filter = null, params Expression<Func<TEntity, object>>[] includes) where TEntity : class
    {
        var key = CacheKey<TEntity>(includes);

        if (filter is null)
        {
            return ReadAllCached<TEntity>(key, includes);
        }

        var rows = _liteDatabase.Database.GetCollection<TEntity>().Query().Where(filter).ToList();
        AttachPoints(rows, includes);
        return rows;
    }

    /// <summary>
    /// Returns the whole collection, scanning it only when the cache is cold or stale. The rows are
    /// shared, the returned list is a fresh copy so a caller cannot corrupt the cached table.
    /// </summary>
    private List<TEntity> ReadAllCached<TEntity>((Type Entity, string Includes) key, Expression<Func<TEntity, object>>[] includes) where TEntity : class
    {
        if (TryGetCached<TEntity>(key, out var cached))
        {
            return cached;
        }

        lock (scanGate)
        {
            if (TryGetCached<TEntity>(key, out cached))
            {
                return cached;
            }

            // Sampled immediately before the scan: a write that lands while the scan is running bumps
            // its type's version past that sample, so the entry is treated as stale on the next read
            // instead of handing out rows that were already outdated when they were stored.
            var dependencies = Dependencies<TEntity>(includes);
            var versions = dependencies.Select(VersionOf).ToArray();

            var rows = _liteDatabase.Database.GetCollection<TEntity>().FindAll().ToList();
            AttachPoints(rows, includes);

            fullReadCache[key] = new CacheEntry(dependencies, versions, rows.Cast<object>().ToList());
            return rows;
        }
    }

    /// <summary>
    /// Attaches the stored GPS points to the trips of a read. A no-op for every other entity type and
    /// for a read that did not ask for the points.
    /// </summary>
    private void AttachPoints<TEntity>(List<TEntity> rows, Expression<Func<TEntity, object>>[] includes) where TEntity : class
    {
        if (rows is List<Trip> trips && includes.Any(IsLocationsInclude))
        {
            _tripPoints.AttachPoints(trips);
        }
    }

    private static bool IsLocationsInclude(LambdaExpression include) =>
        NavigationType(include) == typeof(Location);

    /// <summary>
    /// The element type of the navigation an include names - <c>Location</c> for
    /// <c>trip =&gt; trip.Locations</c>.
    /// </summary>
    private static Type NavigationType(LambdaExpression include)
    {
        var member = include.Body as MemberExpression
            ?? (include.Body as UnaryExpression)?.Operand as MemberExpression;

        if (member?.Type is not { } type)
        {
            return typeof(object);
        }

        return type.IsGenericType ? type.GetGenericArguments()[0] : type;
    }

    private static (Type Entity, string Includes) CacheKey<TEntity>(Expression<Func<TEntity, object>>[] includes) =>
        (typeof(TEntity), string.Join("|", includes.Select(include => include.ToString())));

    /// <summary>
    /// The entity types a cached read was derived from: the entity itself and the navigations it
    /// pulled in. Writing any of those makes the entry stale, writing anything else does not.
    /// </summary>
    private static Type[] Dependencies<TEntity>(Expression<Func<TEntity, object>>[] includes) where TEntity : class =>
        [typeof(TEntity), .. includes.Select(NavigationType).Where(type => type != typeof(object))];

    private void MarkWritten(Type type)
    {
        Interlocked.Increment(ref dataVersion);
        writeVersions.AddOrUpdate(type, 1, static (_, version) => version + 1);
    }

    private bool TryGetCached<TEntity>((Type Entity, string Includes) key, out List<TEntity> rows) where TEntity : class
    {
        if (fullReadCache.TryGetValue(key, out var entry) && IsCurrent(entry))
        {
            rows = entry.Rows.Cast<TEntity>().ToList();
            return true;
        }

        rows = null!;
        return false;
    }

    private bool IsCurrent(CacheEntry entry)
    {
        for (var index = 0; index < entry.Dependencies.Length; index++)
        {
            if (entry.Versions[index] != VersionOf(entry.Dependencies[index]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>One cached full read: the rows plus the versions of everything they were derived from.</summary>
    private sealed record CacheEntry(Type[] Dependencies, long[] Versions, List<object> Rows);
}

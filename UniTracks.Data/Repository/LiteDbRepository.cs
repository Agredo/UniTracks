using System.Collections.Concurrent;
using System.Linq.Expressions;
using LiteDB;
using UniTracks.Data.LiteDB;
using ILiteDatabase = UniTracks.Data.LiteDB.ILiteDatabase;

namespace UniTracks.Data.Repository;

/// <summary>
/// LiteDB-backed repository used on iOS (CoreCLR + ReadyToRun, IsDynamicCodeSupported=false),
/// where EF Core cannot run Database.Migrate() or build its model at runtime. Related entities
/// (e.g. a trip's Locations) are stored as embedded aggregates inside the parent document, so
/// the Include() semantics of the EF repository are a no-op here.
/// </summary>
public class LiteDbRepository : IRepository
{
    private readonly ILiteDatabase _liteDatabase;

    // Full-collection reads are the most expensive operation in the app: every trip document
    // carries all of its GPS points, so FindAll() materialises thousands of Location objects and
    // costs roughly two seconds on device. The statistics page, the games tab, the user page and
    // the trip list each used to run that same scan on every visit - one after the other.
    //
    // A read-through cache per entity type collapses them into a single scan. Entries are stamped
    // with DataVersion, so any write invalidates every cached table. Includes are deliberately not
    // part of the key: they are a no-op here (see the class comment) and including them would give
    // the same list three different cache slots.
    private readonly ConcurrentDictionary<Type, (long Version, List<object> Rows)> fullReadCache = new();

    // Serialises the scans themselves, so two cold readers do not run the same expensive scan
    // in parallel. Only misses take this lock, cache hits never block.
    private readonly object scanGate = new();

    public LiteDbRepository(ILiteDatabase liteDatabase)
    {
        _liteDatabase = liteDatabase;
        DatabasePath = liteDatabase.DatabasePath;
    }

    public string DatabasePath { get; }

    private long dataVersion;

    /// <inheritdoc />
    public long DataVersion => Interlocked.Read(ref dataVersion);

    public Task<TEntity> Add<TEntity>(TEntity entity) where TEntity : class
    {
        _liteDatabase.Database.GetCollection<TEntity>().Insert(entity);
        Interlocked.Increment(ref dataVersion);
        return Task.FromResult(entity);
    }

    public Task<TEntity> Update<TEntity>(TEntity entity) where TEntity : class
    {
        _liteDatabase.Database.GetCollection<TEntity>().Update(entity);
        Interlocked.Increment(ref dataVersion);
        return Task.FromResult(entity);
    }

    public Task Delete<TEntity>(TEntity entity) where TEntity : class
    {
        DeleteCore(entity);
        Interlocked.Increment(ref dataVersion);
        return Task.CompletedTask;
    }

    public Task DeleteRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
    {
        foreach (var entity in entities)
        {
            DeleteCore(entity);
        }

        Interlocked.Increment(ref dataVersion);
        return Task.CompletedTask;
    }

    private void DeleteCore<TEntity>(TEntity entity) where TEntity : class
    {
        var id = typeof(TEntity).GetProperty("ID")?.GetValue(entity)
            ?? throw new InvalidOperationException($"Entity {typeof(TEntity).Name} has no ID property.");
        _liteDatabase.Database.GetCollection<TEntity>().Delete(new BsonValue(id));
    }

    public Task<TEntity?> GetByIdAsync<TEntity>(Guid id) where TEntity : class
    {
        var collection = _liteDatabase.Database.GetCollection<TEntity>();

        // Off the calling thread: a single trip document carries all of its GPS points, so
        // FindById() is not the cheap lookup it looks like (see GetAllAsync below).
        return Task.Run(() => collection.FindById(id));
    }

    public Task<IEnumerable<TEntity>> GetAllAsync<TEntity>(params Expression<Func<TEntity, object>>[] includes) where TEntity : class
    {
        // Distinct from the EF repository: related entities (e.g. a trip's Locations) are embedded
        // aggregates in the parent document, so they are always returned and includes are a no-op.
        var collection = _liteDatabase.Database.GetCollection<TEntity>();

        // A warm cache is answered on the caller's thread: the documents are already materialised,
        // so there is nothing left to move off the UI thread - and on device the thread hop itself
        // added a visible delay to every page switch.
        if (TryGetCached<TEntity>(typeof(TEntity), DataVersion, out var cached))
        {
            return Task.FromResult<IEnumerable<TEntity>>(cached);
        }

        // The read (and the BsonMapper pass that materialises the entities) must happen on the
        // worker thread, not on the caller's. LiteDB reads are synchronous, so returning a lazy
        // ReadOnlyCollection here would run the whole scan on the UI thread - either inside this
        // call or at the first enumeration. Both freeze the UI on every page switch, because
        // FindAll() is the entry point of every view model that loads data.
        return Task.Run(() => (IEnumerable<TEntity>)ReadAllCached<TEntity>(collection));
    }

    public Task<IEnumerable<TEntity>> GetAsync<TEntity>(Expression<Func<TEntity, bool>>? filter = null, params Expression<Func<TEntity, object>>[] includes) where TEntity : class
    {
        var collection = _liteDatabase.Database.GetCollection<TEntity>();

        if (filter is null)
        {
            // Same result as GetAllAsync, so it shares its cache entry instead of scanning again.
            if (TryGetCached<TEntity>(typeof(TEntity), DataVersion, out var cached))
            {
                return Task.FromResult<IEnumerable<TEntity>>(cached);
            }

            return Task.Run(() => (IEnumerable<TEntity>)ReadAllCached<TEntity>(collection));
        }

        // Same reason as GetAllAsync: the query runs synchronously, so it has to leave the caller's
        // thread. Every filter is evaluated by the document store, which means a full collection scan.
        return Task.Run(() => (IEnumerable<TEntity>)collection.Query().Where(filter).ToList());
    }

    public IEnumerable<TEntity> Get<TEntity>(Expression<Func<TEntity, bool>>? filter = null, params Expression<Func<TEntity, object>>[] includes) where TEntity : class
    {
        var collection = _liteDatabase.Database.GetCollection<TEntity>();

        if (filter is null)
        {
            return ReadAllCached<TEntity>(collection);
        }

        return collection.Query().Where(filter).ToList();
    }

    /// <summary>
    /// Returns the whole collection, scanning it only when the cache is cold or stale. The rows are
    /// shared, the returned list is a fresh copy so a caller cannot corrupt the cached table.
    /// </summary>
    private List<TEntity> ReadAllCached<TEntity>(ILiteCollection<TEntity> collection) where TEntity : class
    {
        var key = typeof(TEntity);

        if (TryGetCached<TEntity>(key, DataVersion, out var cached))
        {
            return cached;
        }

        lock (scanGate)
        {
            // The version is read immediately before the scan: a write that lands while the scan is
            // running bumps DataVersion past it, so the entry is treated as stale on the next read
            // instead of handing out rows that were already outdated when they were stored.
            var version = DataVersion;

            if (TryGetCached<TEntity>(key, version, out cached))
            {
                return cached;
            }

            var rows = collection.FindAll().ToList();
            fullReadCache[key] = (version, rows.Cast<object>().ToList());
            return rows;
        }
    }

    private bool TryGetCached<TEntity>(Type key, long version, out List<TEntity> rows) where TEntity : class
    {
        if (fullReadCache.TryGetValue(key, out var entry) && entry.Version == version)
        {
            rows = entry.Rows.Cast<TEntity>().ToList();
            return true;
        }

        rows = null!;
        return false;
    }
}

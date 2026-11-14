using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using UniTracks.Data.SQLite;

namespace UniTracks.Data.Repository;

/// <summary>
/// EF Core + SQLite repository. Used on platforms where JIT is available (Android,
/// Mac Catalyst, Windows) — there EF Core can build its model at runtime and run
/// Database.Migrate(). Relationships are expressed as EF navigations and loaded with Include().
/// </summary>
public class EfRepository : IRepository
{
    private readonly SqliteDBContext _context;

    // A DbContext is not thread-safe, but this repository is a singleton: the UI layer, the
    // fire-and-forget startup distance recalculation and the platform location callback all reach
    // it concurrently. That surfaced as "A second operation was started on this context". Every
    // operation is serialized through this gate so the context stays single-threaded.
    //
    // The awaits below deliberately use ConfigureAwait(false): Get<TEntity>() blocks the calling
    // thread (it has a synchronous signature and is called from the UI thread), so a continuation
    // that had to be posted back to the UI thread to reach its "gate.Release()" would deadlock
    // against it.
    private readonly SemaphoreSlim gate = new(1, 1);

    // Read-through cache for whole-table reads, same purpose as the one in LiteDbRepository: the
    // statistics page, the games tab and the user page each re-read the same tables on every visit.
    // Here the include set is part of the key, because on EF it decides which navigations are
    // materialised - a no-include list must never be handed out for an include request. A cached read
    // also knows which entity types it was derived from, so writing the GPS point of a running
    // recording no longer throws away the cached trip list.
    private readonly ConcurrentDictionary<(Type Entity, string Includes), CacheEntry> fullReadCache = new();

    // Write counter per entity type - the granularity the cache above needs. The global DataVersion
    // is still bumped on every write, because consumers outside the repository use it as a
    // "something changed" stamp for their own caches.
    private readonly ConcurrentDictionary<Type, long> writeVersions = new();

    private long dataVersion;

    public EfRepository(SqliteDBContext context)
    {
        _context = context;
    }

    public string DatabasePath => _context.DatabasePath;

    /// <inheritdoc />
    public long DataVersion => Interlocked.Read(ref dataVersion);

    /// <summary>The number of writes the given entity type has seen; 0 when it was never written.</summary>
    private long VersionOf(Type type) => writeVersions.TryGetValue(type, out var version) ? version : 0;

    public async Task<TEntity> Add<TEntity>(TEntity entity) where TEntity : class
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var entry = _context.Set<TEntity>().Add(entity).Entity;
            await _context.SaveChangesAsync().ConfigureAwait(false);
            MarkWritten<TEntity>();
            return entry;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<TEntity> Update<TEntity>(TEntity entity) where TEntity : class
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            // Update only the root entity's own (scalar) properties. A full _context.Update(entity)
            // would also mark every reachable child (e.g. a Trip's growing Locations collection) as
            // Modified. A newly-created Location row that does not exist in the store yet would then
            // be the target of an UPDATE ... WHERE ID = <new guid>, which affects 0 rows and throws
            // DbUpdateConcurrencyException. Child rows are persisted independently as they arrive.
            var entry = _context.Entry(entity);
            if (entry.State == EntityState.Detached)
            {
                // Attach the root and its already-loaded graph so it is tracked in Unchanged state.
                _context.Attach(entity);
            }

            foreach (var property in entry.Properties)
            {
                // Marking the primary key (or any key) property as modified is not allowed and throws.
                if (!property.Metadata.IsPrimaryKey())
                {
                    property.IsModified = true;
                }
            }

            await _context.SaveChangesAsync().ConfigureAwait(false);
            MarkWritten<TEntity>();
            return entity;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task Delete<TEntity>(TEntity entity) where TEntity : class
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            _context.Remove(entity);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            MarkWritten<TEntity>();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task DeleteRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            _context.RemoveRange(entities);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            MarkWritten<TEntity>();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<TEntity?> GetByIdAsync<TEntity>(Guid id) where TEntity : class
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            return await _context.Set<TEntity>().FindAsync(id).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync<TEntity>(params Expression<Func<TEntity, object>>[] includes) where TEntity : class
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            return await ReadAllCachedAsync<TEntity>(includes).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<IEnumerable<TEntity>> GetAsync<TEntity>(Expression<Func<TEntity, bool>>? filter = null, params Expression<Func<TEntity, object>>[] includes) where TEntity : class
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            IQueryable<TEntity> query = _context.Set<TEntity>();
            if (filter is not null)
            {
                return await query.Where(filter).IncludeMultiple(includes).ToListAsync().ConfigureAwait(false);
            }

            // An unfiltered query is the same result as GetAllAsync, so it shares its cache entry.
            return await ReadAllCachedAsync<TEntity>(includes).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    public IEnumerable<TEntity> Get<TEntity>(Expression<Func<TEntity, bool>>? filter = null, params Expression<Func<TEntity, object>>[] includes) where TEntity : class
    {
        gate.Wait();
        try
        {
            if (filter is not null)
            {
                return _context.Set<TEntity>().Where(filter).IncludeMultiple(includes).ToList();
            }

            return ReadAllCached<TEntity>(includes);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Async variant of <see cref="ReadAllCached{TEntity}"/>. The caller already holds the gate,
    /// which is what keeps this single-threaded against the non-thread-safe <c>DbContext</c>.
    /// </summary>
    private async Task<List<TEntity>> ReadAllCachedAsync<TEntity>(Expression<Func<TEntity, object>>[] includes) where TEntity : class
    {
        var key = CacheKey<TEntity>(includes);
        if (TryGetCached<TEntity>(key, out var cached))
        {
            return cached;
        }

        // Sampled immediately before the read: a write that lands while it is running bumps its
        // type's version past that sample, so the entry is stale on the next visit.
        var dependencies = Dependencies<TEntity>(includes);
        var versions = dependencies.Select(VersionOf).ToArray();

        var rows = await _context.Set<TEntity>().IncludeMultiple(includes).ToListAsync().ConfigureAwait(false);
        fullReadCache[key] = new CacheEntry(dependencies, versions, rows.Cast<object>().ToList());
        return rows;
    }

    private List<TEntity> ReadAllCached<TEntity>(Expression<Func<TEntity, object>>[] includes) where TEntity : class
    {
        var key = CacheKey<TEntity>(includes);
        if (TryGetCached<TEntity>(key, out var cached))
        {
            return cached;
        }

        var dependencies = Dependencies<TEntity>(includes);
        var versions = dependencies.Select(VersionOf).ToArray();

        var rows = _context.Set<TEntity>().IncludeMultiple(includes).ToList();
        fullReadCache[key] = new CacheEntry(dependencies, versions, rows.Cast<object>().ToList());
        return rows;
    }

    private static (Type Entity, string Includes) CacheKey<TEntity>(Expression<Func<TEntity, object>>[] includes) =>
        (typeof(TEntity), string.Join("|", includes.Select(include => include.ToString())));

    /// <summary>
    /// The entity types a cached read was derived from: the entity itself and the navigations it
    /// pulled in. Writing any of those makes the entry stale, writing anything else does not.
    /// </summary>
    private static Type[] Dependencies<TEntity>(Expression<Func<TEntity, object>>[] includes) where TEntity : class =>
        [typeof(TEntity), .. includes.Select(NavigationType).Where(type => type != typeof(object))];

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

    private void MarkWritten<TEntity>() where TEntity : class
    {
        Interlocked.Increment(ref dataVersion);
        writeVersions.AddOrUpdate(typeof(TEntity), 1, static (_, version) => version + 1);
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

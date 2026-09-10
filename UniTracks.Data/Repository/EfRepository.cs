using Microsoft.EntityFrameworkCore;
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

    public EfRepository(SqliteDBContext context)
    {
        _context = context;
    }

    public string DatabasePath => _context.DatabasePath;

    public async Task<TEntity> Add<TEntity>(TEntity entity) where TEntity : class
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var entry = _context.Set<TEntity>().Add(entity).Entity;
            await _context.SaveChangesAsync().ConfigureAwait(false);
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
            return await _context.Set<TEntity>().IncludeMultiple(includes).ToListAsync().ConfigureAwait(false);
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
            IQueryable<TEntity> query = _context.Set<TEntity>();
            if (filter is not null)
            {
                query = query.Where(filter);
            }

            return query.IncludeMultiple(includes).ToList();
        }
        finally
        {
            gate.Release();
        }
    }
}

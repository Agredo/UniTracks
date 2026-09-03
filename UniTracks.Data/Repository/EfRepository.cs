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

    public EfRepository(SqliteDBContext context)
    {
        _context = context;
    }

    public string DatabasePath => _context.DatabasePath;

    public async Task<TEntity> Add<TEntity>(TEntity entity) where TEntity : class
    {
        var entry = _context.Set<TEntity>().Add(entity).Entity;
        await _context.SaveChangesAsync();
        return entry;
    }

    public async Task<TEntity> Update<TEntity>(TEntity entity) where TEntity : class
    {
        var entry = _context.Update(entity).Entity;
        await _context.SaveChangesAsync();
        return entry;
    }

    public async Task Delete<TEntity>(TEntity entity) where TEntity : class
    {
        _context.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<TEntity?> GetByIdAsync<TEntity>(Guid id) where TEntity : class
    {
        return await _context.Set<TEntity>().FindAsync(id);
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync<TEntity>(params Expression<Func<TEntity, object>>[] includes) where TEntity : class
    {
        return await _context.Set<TEntity>().IncludeMultiple(includes).ToListAsync();
    }

    public IEnumerable<TEntity> Get<TEntity>(Expression<Func<TEntity, bool>>? filter = null, params Expression<Func<TEntity, object>>[] includes) where TEntity : class
    {
        IQueryable<TEntity> query = _context.Set<TEntity>();
        if (filter is not null)
        {
            query = query.Where(filter);
        }

        return query.IncludeMultiple(includes).ToList();
    }
}

using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace UniTracks.Data.Repository;

public class GenericRepository<EDbContext> : IGenericRepository<EDbContext> where EDbContext : DbContext
{
    public EDbContext Context { get; }

    public GenericRepository(EDbContext context)
    {
        Context = context;
    }
    public async Task<TEntity> Add<TEntity>(TEntity entity) where TEntity : class
    {
        TEntity e = Context.Set<TEntity>().Add(entity).Entity;
        await SaveChangesAsync();

        return e;
    }

    public async Task<TEntity> Update<TEntity>(TEntity entity) where TEntity : class
    {
        TEntity e = Context.Update(entity).Entity;
        await SaveChangesAsync();

        return e;
    }

    public async Task Delete<TEntity>(TEntity entity) where TEntity : class
    {
        Context.Remove(entity);
        await SaveChangesAsync();
    }

    public virtual IEnumerable<TEntity> Get<TEntity>(
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            params Expression<Func<TEntity, object>>[] includes) where TEntity : class
    {
        IQueryable<TEntity> query = Context.Set<TEntity>();

        if (filter != null)
        {
            query = query.Where(filter);
        }
        if (orderBy != null)
        {
            return orderBy(query).ToList();
        }
        else
        {
            query.IncludeMultiple(includes);
            return query.ToList();
        }
    }

    public async Task<TEntity> GetByIdAsync<TEntity>(int id) where TEntity : class
    {
        return await Context.Set<TEntity>().FindAsync(id);
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync<TEntity>(params Expression<Func<TEntity, object>>[] includes) where TEntity : class
    {
        return await Context.Set<TEntity>().IncludeMultiple(includes).ToListAsync();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await Context.SaveChangesAsync();
    }
}

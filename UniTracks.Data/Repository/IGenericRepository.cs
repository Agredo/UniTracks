using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace UniTracks.Data.Repository;

public interface IGenericRepository<EDbContext> where EDbContext : DbContext
{
    EDbContext Context { get; }

    Task<TEntity> Add<TEntity>(TEntity entity) where TEntity : class;
    Task Delete<TEntity>(TEntity entity) where TEntity : class;
    IEnumerable<TEntity> Get<TEntity>(Expression<Func<TEntity, bool>> filter = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null, params Expression<Func<TEntity, object>>[] includes) where TEntity : class;
    Task<IEnumerable<TEntity>> GetAllAsync<TEntity>(params Expression<Func<TEntity, object>>[] includes) where TEntity : class;
    Task<TEntity> GetByIdAsync<TEntity>(int id) where TEntity : class;
    Task<int> SaveChangesAsync();
    Task<TEntity> Update<TEntity>(TEntity entity) where TEntity : class;
}

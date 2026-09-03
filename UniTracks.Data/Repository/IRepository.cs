using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace UniTracks.Data.Repository;

/// <summary>
/// Provider-agnostic data-access contract used by the UI layer. On iOS it is backed by
/// LiteDB (document store with embedded aggregates); on all other platforms it is backed by
/// EF Core + SQLite. ViewModels and services depend only on this abstraction.
/// </summary>
public interface IRepository
{
    string DatabasePath { get; }

    Task<TEntity> Add<TEntity>(TEntity entity) where TEntity : class;
    Task<TEntity> Update<TEntity>(TEntity entity) where TEntity : class;
    Task Delete<TEntity>(TEntity entity) where TEntity : class;
    Task<TEntity?> GetByIdAsync<TEntity>(Guid id) where TEntity : class;
    Task<IEnumerable<TEntity>> GetAllAsync<TEntity>(params Expression<Func<TEntity, object>>[] includes) where TEntity : class;
    IEnumerable<TEntity> Get<TEntity>(Expression<Func<TEntity, bool>>? filter = null, params Expression<Func<TEntity, object>>[] includes) where TEntity : class;
}

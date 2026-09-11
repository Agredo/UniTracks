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

    /// <summary>
    /// Monotonically increasing counter, incremented on every write (add/update/delete). Read-side
    /// caches compare against it to tell whether their data is still current — without it they would
    /// have to re-read the whole store, which is expensive (a trip document carries all of its GPS
    /// points, and on iOS the BsonMapper pass over those documents is the single most costly
    /// operation in the app).
    /// </summary>
    long DataVersion { get; }

    Task<TEntity> Add<TEntity>(TEntity entity) where TEntity : class;
    Task<TEntity> Update<TEntity>(TEntity entity) where TEntity : class;
    Task Delete<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>
    /// Deletes a batch of entities in one operation. Needed for child rows that the store does not
    /// cascade (e.g. a trip's locations), so deleting a parent does not leave orphans behind.
    /// </summary>
    Task DeleteRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;

    Task<TEntity?> GetByIdAsync<TEntity>(Guid id) where TEntity : class;
    Task<IEnumerable<TEntity>> GetAllAsync<TEntity>(params Expression<Func<TEntity, object>>[] includes) where TEntity : class;

    /// <summary>
    /// Filtered read. Unlike <see cref="Get{TEntity}"/>, which runs the query on the calling thread,
    /// this one does the work off the caller's thread — the stores are synchronous, so the synchronous
    /// overload blocks the UI thread for the whole scan.
    /// </summary>
    Task<IEnumerable<TEntity>> GetAsync<TEntity>(Expression<Func<TEntity, bool>>? filter = null, params Expression<Func<TEntity, object>>[] includes) where TEntity : class;

    IEnumerable<TEntity> Get<TEntity>(Expression<Func<TEntity, bool>>? filter = null, params Expression<Func<TEntity, object>>[] includes) where TEntity : class;
}

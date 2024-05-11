using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace UniTracks.Data.Repository;

/// <summary>
/// Represents a generic repository interface for working with entities in a database context.
/// </summary>
/// <typeparam name="EDbContext">The type of the database context.</typeparam>
public interface IGenericRepository<EDbContext> where EDbContext : DbContext
{
    /// <summary>
    /// Gets the database context associated with the repository.
    /// </summary>
    EDbContext Context { get; }

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <param name="entity">The entity to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<TEntity> Add<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>
    /// Deletes an entity from the repository.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <param name="entity">The entity to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task Delete<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>
    /// Gets entities from the repository based on the specified filter, order, and includes.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <param name="filter">The filter expression.</param>
    /// <param name="orderBy">The order expression.</param>
    /// <param name="includes">The include expressions.</param>
    /// <returns>The collection of entities.</returns>
    IEnumerable<TEntity> Get<TEntity>(Expression<Func<TEntity, bool>> filter = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null, params Expression<Func<TEntity, object>>[] includes) where TEntity : class;

    /// <summary>
    /// Gets all entities from the repository with the specified includes.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <param name="includes">The include expressions.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<IEnumerable<TEntity>> GetAllAsync<TEntity>(params Expression<Func<TEntity, object>>[] includes) where TEntity : class;

    /// <summary>
    /// Gets an entity from the repository by its ID.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <param name="id">The ID of the entity.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<TEntity> GetByIdAsync<TEntity>(int id) where TEntity : class;

    /// <summary>
    /// Saves changes made to the repository.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<int> SaveChangesAsync();

    /// <summary>
    /// Updates an entity in the repository.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <param name="entity">The entity to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<TEntity> Update<TEntity>(TEntity entity) where TEntity : class;
}

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

    public LiteDbRepository(ILiteDatabase liteDatabase)
    {
        _liteDatabase = liteDatabase;
        DatabasePath = liteDatabase.DatabasePath;
    }

    public string DatabasePath { get; }

    public Task<TEntity> Add<TEntity>(TEntity entity) where TEntity : class
    {
        _liteDatabase.Database.GetCollection<TEntity>().Insert(entity);
        return Task.FromResult(entity);
    }

    public Task<TEntity> Update<TEntity>(TEntity entity) where TEntity : class
    {
        _liteDatabase.Database.GetCollection<TEntity>().Update(entity);
        return Task.FromResult(entity);
    }

    public Task Delete<TEntity>(TEntity entity) where TEntity : class
    {
        DeleteCore(entity);
        return Task.CompletedTask;
    }

    public Task DeleteRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
    {
        foreach (var entity in entities)
        {
            DeleteCore(entity);
        }

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

        // The read (and the BsonMapper pass that materialises the entities) must happen on the
        // worker thread, not on the caller's. LiteDB reads are synchronous, so returning a lazy
        // ReadOnlyCollection here would run the whole scan on the UI thread - either inside this
        // call or at the first enumeration. Both freeze the UI on every page switch, because
        // FindAll() is the entry point of every view model that loads data.
        return Task.Run(() => (IEnumerable<TEntity>)collection.FindAll().ToList());
    }

    public Task<IEnumerable<TEntity>> GetAsync<TEntity>(Expression<Func<TEntity, bool>>? filter = null, params Expression<Func<TEntity, object>>[] includes) where TEntity : class
    {
        var collection = _liteDatabase.Database.GetCollection<TEntity>();

        // Same reason as GetAllAsync: the query runs synchronously, so it has to leave the caller's
        // thread. Every filter is evaluated by the document store, which means a full collection scan.
        return Task.Run(
            () => (IEnumerable<TEntity>)(filter is null
                ? collection.Query().ToList()
                : collection.Query().Where(filter).ToList()));
    }

    public IEnumerable<TEntity> Get<TEntity>(Expression<Func<TEntity, bool>>? filter = null, params Expression<Func<TEntity, object>>[] includes) where TEntity : class
    {
        var query = _liteDatabase.Database.GetCollection<TEntity>().Query();
        if (filter is not null)
        {
            query = query.Where(filter);
        }

        return query.ToList();
    }
}

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
        var id = typeof(TEntity).GetProperty("ID")?.GetValue(entity)
            ?? throw new InvalidOperationException($"Entity {typeof(TEntity).Name} has no ID property.");
        _liteDatabase.Database.GetCollection<TEntity>().Delete(new BsonValue(id));
        return Task.CompletedTask;
    }

    public Task<TEntity?> GetByIdAsync<TEntity>(Guid id) where TEntity : class
    {
        TEntity? entity = _liteDatabase.Database.GetCollection<TEntity>().FindById(id);
        return Task.FromResult(entity);
    }

    public Task<IEnumerable<TEntity>> GetAllAsync<TEntity>(params Expression<Func<TEntity, object>>[] includes) where TEntity : class
    {
        IEnumerable<TEntity> entities = _liteDatabase.Database.GetCollection<TEntity>().FindAll();
        return Task.FromResult(entities);
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

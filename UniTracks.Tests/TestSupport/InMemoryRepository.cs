using System.Linq.Expressions;
using UniTracks.Data.Repository;

namespace UniTracks.Tests.TestSupport;

/// <summary>
/// An <see cref="IRepository"/> that keeps everything in lists and, unlike every real store, permits
/// duplicate keys. It exists to reproduce store states a healthy database can no longer reach — such as
/// the duplicate fingerprints an earlier iOS build wrote — so the code that has to cope with them can
/// be tested at all.
/// </summary>
internal sealed class InMemoryRepository : IRepository
{
    private readonly Dictionary<Type, List<object>> rows = new();

    public string DatabasePath => ":memory:";

    public long DataVersion { get; private set; }

    /// <summary>Entities removed through the repository, so a test can assert on the cleanup.</summary>
    public List<object> Deleted { get; } = new();

    /// <summary>Adds without the duplicate check the real stores apply.</summary>
    public void Seed<TEntity>(params TEntity[] entities) where TEntity : class
    {
        var list = Rows<TEntity>();
        list.AddRange(entities);
        DataVersion++;
    }

    public Task<TEntity> Add<TEntity>(TEntity entity) where TEntity : class
    {
        Rows<TEntity>().Add(entity);
        DataVersion++;
        return Task.FromResult(entity);
    }

    public Task<TEntity> Update<TEntity>(TEntity entity) where TEntity : class
    {
        var list = Rows<TEntity>();
        int index = list.FindIndex(row => Key(row) == Key(entity));
        if (index >= 0)
        {
            list[index] = entity;
        }
        else
        {
            list.Add(entity);
        }

        DataVersion++;
        return Task.FromResult(entity);
    }

    public Task Delete<TEntity>(TEntity entity) where TEntity : class
    {
        Rows<TEntity>().RemoveAll(row => ReferenceEquals(row, entity));
        Deleted.Add(entity);
        DataVersion++;
        return Task.CompletedTask;
    }

    public Task DeleteRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
    {
        foreach (var entity in entities)
        {
            Delete(entity);
        }

        return Task.CompletedTask;
    }

    public Task<TEntity?> GetByIdAsync<TEntity>(Guid id) where TEntity : class =>
        Task.FromResult(Rows<TEntity>().FirstOrDefault(row => Key(row) == id) as TEntity);

    public Task<IEnumerable<TEntity>> GetAllAsync<TEntity>(params Expression<Func<TEntity, object>>[] includes) where TEntity : class =>
        Task.FromResult<IEnumerable<TEntity>>(Rows<TEntity>().Cast<TEntity>().ToList());

    public Task<IEnumerable<TEntity>> GetAsync<TEntity>(
        Expression<Func<TEntity, bool>>? filter = null,
        params Expression<Func<TEntity, object>>[] includes) where TEntity : class =>
        Task.FromResult(Get(filter, includes));

    public IEnumerable<TEntity> Get<TEntity>(
        Expression<Func<TEntity, bool>>? filter = null,
        params Expression<Func<TEntity, object>>[] includes) where TEntity : class
    {
        var query = Rows<TEntity>().Cast<TEntity>();

        return filter is null ? query.ToList() : query.Where(filter.Compile()).ToList();
    }

    private List<object> Rows<TEntity>() where TEntity : class
    {
        if (!rows.TryGetValue(typeof(TEntity), out var list))
        {
            list = new List<object>();
            rows[typeof(TEntity)] = list;
        }

        return list;
    }

    private static Guid? Key(object entity) =>
        entity.GetType().GetProperty("ID")?.GetValue(entity) as Guid?
        ?? entity.GetType().GetProperty("TripID")?.GetValue(entity) as Guid?;
}

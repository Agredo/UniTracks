using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using UniTracks.Services.IO;
using ILiteDatabase = UniTracks.Data.LiteDB.ILiteDatabase;

namespace UniTracks.Data.Repository;

public class GenericLiteDBRepository<ELiteDatabase> : IGenericLiteDBRepository<ELiteDatabase> where ELiteDatabase : ILiteDatabase
{
    public ELiteDatabase LiteDatabase { get; private set; }

    public GenericLiteDBRepository(ELiteDatabase liteDatabase)
    {
        LiteDatabase = liteDatabase;
    }

    public async Task Add<TEntity>(TEntity entity) where TEntity : class
    {
        LiteDatabase.Database.GetCollection<TEntity>().Insert(entity);
    }

    public async Task<bool> Delete<TEntity>(Guid id) where TEntity : class
    {
        return LiteDatabase.Database.GetCollection<TEntity>().Delete(id);  
    }

    public IEnumerable<TEntity> Get<TEntity>(Expression<Func<TEntity, bool>> filter = null, BsonExpression orderBy = null) where TEntity : class
    {
        ILiteQueryable<TEntity> query = LiteDatabase.Database.GetCollection<TEntity>().Query();

        if (filter != null)
        {
            query = query.Where(filter);
        }

        if (orderBy != null)
        {
            return query.OrderBy(orderBy).ToList();
        }
        else
        {
            return query.ToList();
        }
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync<TEntity>() where TEntity : class
    {
        return LiteDatabase.Database.GetCollection<TEntity>().FindAll();
    }

    public async Task<TEntity> GetByIdAsync<TEntity>(int id) where TEntity : class
    {
        return LiteDatabase.Database.GetCollection<TEntity>().FindById(id);
    }

    public async Task Update<TEntity>(TEntity entity) where TEntity : class
    {
        LiteDatabase.Database.GetCollection<TEntity>().Update(entity);
    }

    public void Dispose()
    {
        LiteDatabase.Database.Dispose();
    }
}

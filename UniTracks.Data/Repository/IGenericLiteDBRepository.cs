using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using ILiteDatabase = UniTracks.Data.LiteDB.ILiteDatabase;

namespace UniTracks.Data.Repository;

public interface IGenericLiteDBRepository<ELiteDatabase> where ELiteDatabase : ILiteDatabase
{
    public ELiteDatabase LiteDatabase { get; }
    Task Add<TEntity>(TEntity entity) where TEntity : class;
    Task<bool> Delete<TEntity>(Guid id) where TEntity : class;
    IEnumerable<TEntity> Get<TEntity>(Expression<Func<TEntity, bool>> filter = null, BsonExpression orderBy = null) where TEntity : class;
    Task<IEnumerable<TEntity>> GetAllAsync<TEntity>() where TEntity : class;
    Task<TEntity> GetByIdAsync<TEntity>(int id) where TEntity : class;
    Task Update<TEntity>(TEntity entity) where TEntity : class;

    void Dispose();
}

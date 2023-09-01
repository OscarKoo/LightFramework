using System.Linq.Expressions;
using Dao.LightFramework.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dao.LightFramework.EntityFrameworkCore.DataProviders;

public interface IDbRepository<TEntity> : IRepository
    where TEntity : Entity, new()
{
    DbSet<TEntity> DbSet { get; }
    IQueryable<TEntity> DbQuery(bool asNoTracking = false, bool filterSite = true, string site = null);
    Task<int> SaveChangesAsync(bool autoSave);

    #region Entity

    Task<TEntity> GetAsync(string id, bool asNoTracking = false, params string[] cacheKeys);
    Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> where, bool asNoTracking = false, params string[] cacheKeys);
    Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> where, bool asNoTracking = false, params string[] cacheKeys);

    Task<TEntity> SaveAsync(TEntity entity, bool autoSave = false);
    Task<ICollection<TEntity>> SaveManyAsync(ICollection<TEntity> entities, bool autoSave = false);
    Task<int> DeleteAsync(TEntity entity, bool autoSave = false);
    Task<int> DeleteManyAsync(ICollection<TEntity> entities, bool autoSave = false);
    Task<int> DeleteAsync(Expression<Func<TEntity, bool>> where, bool autoSave = false);

    #endregion

    #region Dto

    Task<TEntity> RetrieveAsync<TDto>(string id, TDto dto, bool ignoreNullValue = true);
    Task<TEntity> SaveDtoAsync<TDto>(TDto dto, TEntity entity = null, bool autoSave = false, bool ignoreNullValue = true, params string[] cacheKeys) where TDto : IId;
    Task<ICollection<TDto>> SaveManyDtoAsync<TDto>(ICollection<TDto> dtos, bool autoSave = false, bool ignoreNullValue = true, params string[] cacheKeys) where TDto : IId;
    Task<int> DeleteDtoAsync<TDto>(TDto dto, TEntity entity = null, bool autoSave = false, bool ignoreNullValue = true, params string[] cacheKeys) where TDto : IId;
    Task<int> DeleteManyDtoAsync<TDto>(ICollection<TDto> dtos, bool autoSave = false, bool ignoreNullValue = true, params string[] cacheKeys) where TDto : IId;

    #endregion

    Task<int> Merge(Expression<Func<TEntity, bool>> where, IEnumerable<TEntity> source, Expression<Func<TEntity, TEntity>> insert = null, Expression<Func<TEntity, TEntity, TEntity>> update = null, Expression<Func<TEntity, TEntity, bool>> updateAnd = null, bool useDelete = true, Expression<Func<TEntity, TEntity>> delete = null);
}

public interface ILockedDbRepository<TEntity> : IDbRepository<TEntity>
    where TEntity : Entity, new()
{
    Task<TEntity> GetOrCreateAsync(string key, Expression<Func<TEntity, bool>> where,
        Func<TEntity> createEntity, Func<DbContext, TEntity, Task> onCreating = null,
        Func<TEntity, bool> requireUpdate = null, Func<DbContext, TEntity, Task<bool>> onUpdating = null,
        bool asNoTracking = false, params string[] cacheKeys);
}
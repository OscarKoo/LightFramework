using System.Linq.Expressions;
using System.Transactions;
using Dao.IndividualLock;
using Dao.LightFramework.Common.Exceptions;
using Dao.LightFramework.Common.Utilities;
using Dao.LightFramework.Domain.Entities;
using Dao.LightFramework.Domain.Utilities;
using Dao.LightFramework.EntityFrameworkCore.Utilities;
using Dao.LightFramework.Services;
using LinqToDB;
using LinqToDB.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Dao.LightFramework.EntityFrameworkCore.DataProviders;

public class DbRepository<TEntity> : ServiceContextServiceBase, IDbRepository<TEntity>
    where TEntity : Entity, new()
{
    protected Microsoft.EntityFrameworkCore.DbContext dbContext;
    protected readonly bool isSoftDelete;

    public DbRepository(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        this.dbContext = _<Microsoft.EntityFrameworkCore.DbContext>();
        this.isSoftDelete = typeof(IDeleted).IsAssignableFrom(typeof(TEntity));
    }

    public Microsoft.EntityFrameworkCore.DbSet<TEntity> DbSet => this.dbContext.Set<TEntity>();

    public IQueryable<TEntity> DbQuery(bool asNoTracking = false, params string[] sites)
    {
        var query = DbSet.AsNoTracking(asNoTracking);
        if (sites != null)
        {
            if (sites.Length > 0)
                sites = sites.Where(w => w != null).Select(s => !string.IsNullOrWhiteSpace(s) ? s : RequestContext.Site ?? string.Empty).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (sites.Length == 0)
                sites = new[] { RequestContext.Site ?? string.Empty };
            query = query.BySite(sites);
        }

        return query;
    }

    public async Task<int> SaveChangesAsync(bool autoSave, bool ignoreRowVersionOnSaving = false)
    {
        if (autoSave && ignoreRowVersionOnSaving && this.dbContext is EFContext)
            DbContextCurrent.Add(IgnoreRowVersionMode.Once);

        return autoSave ? await this.dbContext.SaveChangesAsync() : 0;
    }

    #region Entity

    public async Task<TEntity> GetAsync(string id, bool asNoTracking = false, params string[] cacheKeys) =>
        string.IsNullOrWhiteSpace(id) ? default : await GetAsync(w => w.Id == id, asNoTracking, cacheKeys);

    public async Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> where, bool asNoTracking = false, params string[] cacheKeys) =>
        await GetAsync(where, null, asNoTracking, cacheKeys);

    public Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> where, Func<IQueryable<TEntity>, IQueryable<TEntity>> orderBy, bool asNoTracking = false, params string[] cacheKeys)
    {
        var query = DbQuery(asNoTracking);
        if (where != null)
            query = query.Where(where);
        if (orderBy != null)
            query = orderBy(query);
        return query.FirstOrDefaultFromCacheAsync(asNoTracking, cacheKeys);
    }

    public async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> where, bool asNoTracking = false, params string[] cacheKeys) =>
        await GetListAsync(where, null, asNoTracking, string.Empty, cacheKeys);

    public async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> where, bool asNoTracking, string site, params string[] cacheKeys) =>
        await GetListAsync(where, null, asNoTracking, site, cacheKeys);

    public Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> where, Func<IQueryable<TEntity>, IQueryable<TEntity>> orderBy, bool asNoTracking, string site, params string[] cacheKeys)
    {
        var query = DbQuery(asNoTracking,
            site == null
                ? null
                : string.IsNullOrWhiteSpace(site)
                    ? Array.Empty<string>()
                    : new[] { site });
        if (where != null)
            query = query.Where(where);
        if (orderBy != null)
            query = orderBy(query);
        return query.ToListFromCacheAsync(asNoTracking, cacheKeys);
    }

    public async Task<TEntity> SaveAsync(TEntity entity, bool autoSave = false, bool ignoreRowVersionOnSaving = false)
    {
        if (entity.IsNew)
        {
            if (string.IsNullOrWhiteSpace(entity.Id))
                entity.Id = NextSequentialGuid();
            DbSet.Add(entity);
        }
        //else
        //{
        //    DbSet.Update(entity);
        //}

        await SaveChangesAsync(autoSave, ignoreRowVersionOnSaving);
        return entity;
    }

    public async Task<ICollection<TEntity>> SaveManyAsync(ICollection<TEntity> entities, bool autoSave = false, bool ignoreRowVersionOnSaving = false)
    {
        if (entities.IsNullOrEmpty())
            return entities;

        var insert = entities.Where(w => w.IsNew).Select(s =>
        {
            if (string.IsNullOrWhiteSpace(s.Id))
                s.Id = NextSequentialGuid();
            return s;
        }).ToList();
        //var update = entities.Where(w => !w.IsNew).ToList();
        if (insert.Count > 0)
            DbSet.AddRange(insert);
        //if (update.Count > 0)
        //    DbSet.UpdateRange(update);

        await SaveChangesAsync(autoSave, ignoreRowVersionOnSaving);
        return entities;
    }

    public async Task<int> DeleteAsync(TEntity entity, bool autoSave = false, bool ignoreRowVersionOnSaving = false)
    {
        if (this.isSoftDelete)
        {
            ((IDeleted)entity).IsDeleted = true;
            await SaveAsync(entity, autoSave, ignoreRowVersionOnSaving);
        }
        else
        {
            DbSet.Remove(entity);
            await SaveChangesAsync(autoSave, ignoreRowVersionOnSaving);
        }

        return 1;
    }

    public async Task<int> DeleteManyAsync(ICollection<TEntity> entities, bool autoSave = false, bool ignoreRowVersionOnSaving = false)
    {
        if (this.isSoftDelete)
        {
            foreach (var entity in entities)
                ((IDeleted)entity).IsDeleted = true;

            await SaveManyAsync(entities, autoSave, ignoreRowVersionOnSaving);
        }
        else
        {
            DbSet.RemoveRange(entities);
            await SaveChangesAsync(autoSave, ignoreRowVersionOnSaving);
        }

        return entities.Count;
    }

    public async Task<int> DeleteAsync(Expression<Func<TEntity, bool>> where, bool autoSave = false, bool ignoreRowVersionOnSaving = false)
    {
        var query = DbQuery();
        if (where != null)
            query = query.Where(where);
        var list = await query.ToListAsync();
        await DeleteManyAsync(list, autoSave);
        return list.Count;
    }

    #endregion

    #region Dto

    public async Task<TEntity> RetrieveAsync<TDto>(string id, TDto dto, bool ignoreNullValue = true)
    {
        TEntity entity = null;
        if (!string.IsNullOrWhiteSpace(id))
            entity = await GetAsync(id);
        entity ??= new TEntity().SetIsNew<TEntity>(true);
        return dto.Adapt(entity, ignoreNullValue);
    }

    public async Task<TEntity> SaveDtoAsync<TDto>(TDto dto, bool autoSave = false, bool ignoreNullValue = true, bool ignoreRowVersionOnSaving = false) where TDto : IId => await SaveDtoAsync(dto, null, autoSave, ignoreNullValue, ignoreRowVersionOnSaving);

    public async Task<TEntity> SaveDtoAsync<TDto>(TDto dto, TEntity entity, bool autoSave = false, bool ignoreNullValue = true, bool ignoreRowVersionOnSaving = false) where TDto : IId
    {
        entity ??= await RetrieveAsync(dto.Id, dto, ignoreNullValue);
        var tracker = new EntityDtoTracker();
        tracker.Add(entity, dto);
        if (!autoSave) (this.dbContext as EFContext)?.EntityDtoTracker.Add(entity, dto);
        await SaveAsync(entity, autoSave, ignoreRowVersionOnSaving);
        tracker.MapToDtos();
        return entity;
    }

    public async Task<ICollection<TDto>> SaveManyDtoAsync<TDto>(ICollection<TDto> dtos, bool autoSave = false, bool ignoreNullValue = true, bool ignoreRowVersionOnSaving = false) where TDto : IId
    {
        var tracker = new EntityDtoTracker();
        var entities = await dtos.SelectAsync(async dto =>
        {
            var entity = await RetrieveAsync(dto.Id, dto, ignoreNullValue);
            tracker.Add(entity, dto);
            if (!autoSave) (this.dbContext as EFContext)?.EntityDtoTracker.Add(entity, dto);
            return entity;
        });
        await SaveManyAsync(entities, autoSave);
        tracker.MapToDtos();
        return dtos;
    }

    public async Task<int> DeleteDtoAsync<TDto>(TDto dto, bool autoSave = false, bool ignoreNullValue = true, bool ignoreRowVersionOnSaving = false) where TDto : IId
    {
        if (dto == null)
            return 0;

        var entity = await RetrieveAsync(dto.Id, dto, ignoreNullValue);
        if (entity.IsNew)
            throw new DataHasChangedException(Lang.Get("数据已被其他用户删除. ({0}: {1})", typeof(TDto).Name, dto.Id), null, dto);
        return await DeleteAsync(entity, autoSave, ignoreRowVersionOnSaving);
    }

    public async Task<int> DeleteManyDtoAsync<TDto>(ICollection<TDto> dtos, bool autoSave = false, bool ignoreNullValue = true, bool ignoreRowVersionOnSaving = false) where TDto : IId
    {
        if (dtos.IsNullOrEmpty())
            return 0;

        var entities = await dtos.SelectAsync(async dto => await RetrieveAsync(dto.Id, dto, ignoreNullValue));
        return await DeleteManyAsync(entities, autoSave);
    }

    #endregion

    public async Task<int> Merge(Expression<Func<TEntity, bool>> where, IEnumerable<TEntity> source, Expression<Func<TEntity, TEntity>> insert = null, Expression<Func<TEntity, TEntity, TEntity>> update = null, Expression<Func<TEntity, TEntity, bool>> updateAnd = null, bool useDelete = true, Expression<Func<TEntity, TEntity>> delete = null)
    {
        var merge = DbQuery(true).Where(where).AsCte().Merge().Using(source).OnTargetKey();

        if (insert != null)
            merge = merge.InsertWhenNotMatched(insert);

        if (update != null)
            merge = updateAnd != null
                ? merge.UpdateWhenMatchedAnd(updateAnd, update)
                : merge.UpdateWhenMatched(update);

        if (useDelete)
            merge = delete != null
                ? merge.UpdateWhenNotMatchedBySource(delete)
                : merge.DeleteWhenNotMatchedBySource();

        return await ((IMergeable<TEntity, TEntity>)merge).MergeAsync();
    }
}

public class LockedDbRepository<TEntity> : DbRepository<TEntity>, ILockedDbRepository<TEntity>
    where TEntity : Entity, new()
{
    protected readonly string TableName;

    public LockedDbRepository(IServiceProvider serviceProvider) : base(serviceProvider) => this.TableName = this.dbContext.GetTableName<TEntity>();

    protected static readonly IndividualLocks<string> locks = new(StringComparer.OrdinalIgnoreCase);

    public async Task<TEntity> GetOrCreateAsync(string key, Expression<Func<TEntity, bool>> where,
        Func<TEntity> createEntity, Func<Microsoft.EntityFrameworkCore.DbContext, TEntity, Task> onCreating = null,
        Func<TEntity, bool> requireUpdate = null, Func<Microsoft.EntityFrameworkCore.DbContext, TEntity, Task<bool>> onUpdating = null,
        bool asNoTracking = false, params string[] cacheKeys)
    {
        var query = DbQuery(asNoTracking).Where(where);
        var entity = await query.FirstOrDefaultFromCacheAsync(asNoTracking, cacheKeys);
        if (entity != null && (requireUpdate == null || onUpdating == null || !requireUpdate(entity)))
            return entity;

        using (await locks.LockAsync(key))
        {
            entity = await query.FirstOrDefaultFromCacheAsync(asNoTracking, cacheKeys);
            if (entity != null && (requireUpdate == null || onUpdating == null || !requireUpdate(entity)))
                return entity;

            if (createEntity == null)
                return null;

            await TransactionHelper.ScopeAsync(async () => await ServiceProvider.ScopeAsync(async svc =>
            {
                await using var context = svc.GetRequiredService<Microsoft.EntityFrameworkCore.DbContext>();
                var requireSave = true;
                if (entity == null)
                {
                    entity = createEntity();
                    context.Set<TEntity>().Add(entity);

                    if (onCreating != null)
                        await onCreating(context, entity);
                }
                else
                {
                    requireSave = await onUpdating(context, entity);
                    if (requireSave && asNoTracking)
                        context.Update(entity);
                }

                if (requireSave)
                    await context.SaveChangesAsync();
                return true;
            }), TransactionScopeOption.Suppress);

            entity = await query.FirstOrDefaultFromCacheAsync(asNoTracking, cacheKeys);
            return entity;
        }
    }
}
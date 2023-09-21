using System.Linq.Expressions;
using Dao.LightFramework.Common.Utilities;
using Dao.LightFramework.Domain.Entities;
using Dao.LightFramework.EntityFrameworkCore.DataProviders;
using Mapster;

namespace Dao.LightFramework.Application;

public abstract class CrudAppService<TEntity, TDto> : AppService, ICrudAppService<TEntity, TDto>
    where TEntity : Entity, new()
    where TDto : TEntity
{
    protected readonly bool isEntityDtoSame;
    protected readonly IDbRepository<TEntity> repository;

    protected CrudAppService(IServiceProvider serviceProvider, IDbRepository<TEntity> repository) : base(serviceProvider)
    {
        this.repository = repository;
        this.isEntityDtoSame = typeof(TEntity) == typeof(TDto);
    }

    public virtual async Task<TDto> SaveAsync(TDto dto)
    {
        dto.CheckNull(nameof(TEntity));
        await this.repository.SaveDtoAsync(dto, true, false);
        return dto;
    }

    public virtual async Task<int> DeleteAsync(TDto dto)
    {
        dto.CheckNull(nameof(TEntity));
        return await this.repository.DeleteDtoAsync(dto, autoSave: true);
    }

    public virtual async Task<TDto> GetAsync(string id, params string[] cacheKeys)
    {
        var entity = await this.repository.GetAsync(id, true, cacheKeys);
        return this.isEntityDtoSame
            ? (TDto)entity
            : entity?.Adapt<TDto>();
    }

    public virtual async Task<TDto> GetAsync(Expression<Func<TEntity, bool>> where, params string[] cacheKeys)
    {
        var entity = await this.repository.GetAsync(where, true, cacheKeys);
        return this.isEntityDtoSame
            ? (TDto)entity
            : entity?.Adapt<TDto>();
    }

    public virtual async Task<List<TDto>> GetListAsync(Expression<Func<TEntity, bool>> where, params string[] cacheKeys)
    {
        var data = await this.repository.GetListAsync(where, true, cacheKeys);
        return this.isEntityDtoSame
            ? data as List<TDto>
            : data?.AdaptList<TDto>();
    }
}
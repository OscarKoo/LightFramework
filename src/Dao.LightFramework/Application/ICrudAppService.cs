using Dao.LightFramework.Domain.Entities;
using System.Linq.Expressions;

namespace Dao.LightFramework.Application;

public interface ICrudAppService<TEntity, TDto> : IAppService
    where TEntity : Entity
    where TDto : TEntity
{
    Task<TDto> SaveAsync(TDto dto);
    Task<int> DeleteAsync(TDto dto);
    Task<TDto> GetAsync(string id, params string[] cacheKeys);
    Task<TDto> GetAsync(Expression<Func<TEntity, bool>> where, params string[] cacheKeys);
    Task<List<TDto>> GetListAsync(Expression<Func<TEntity, bool>> where, params string[] cacheKeys);
}
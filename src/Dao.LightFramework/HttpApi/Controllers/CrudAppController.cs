using Dao.LightFramework.Application;
using Dao.LightFramework.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Dao.LightFramework.HttpApi.Controllers;

public abstract class CrudAppController<TIAppService, TEntity, TDto> : AppController<TIAppService>
    where TIAppService : IAppService, ICrudAppService<TEntity, TDto>
    where TEntity : Entity, new()
    where TDto : TEntity
{
    protected CrudAppController(TIAppService appService) : base(appService) { }

    [Route(""), HttpPost, HttpPut]
    public async Task<TDto> Save([FromBody] TDto data) =>
        await this.appService.SaveAsync(data);

    [Route(""), HttpDelete]
    public async Task<int> Delete([FromBody] TDto data) =>
        await this.appService.DeleteAsync(data);

    [Route("{id}"), HttpGet]
    public async Task<TDto> Get(string id) =>
        await this.appService.GetAsync(id);
}
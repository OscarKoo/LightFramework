using Dao.LightFramework.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dao.LightFramework.HttpApi.Controllers;

[Authorize]
[ApiController]
public abstract class AppController : ControllerBase { }

public abstract class AppController<TIAppService> : AppController where TIAppService : IAppService
{
    protected TIAppService appService;

    protected AppController(TIAppService appService) => this.appService = appService;

    protected async Task<string> ReadRequestBody()
    {
        Request.Body.Seek(0, SeekOrigin.Begin);
        using var sr = new StreamReader(Request.Body);
        return await sr.ReadToEndAsync();
    }
}
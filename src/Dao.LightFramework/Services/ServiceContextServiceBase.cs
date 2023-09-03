using Dao.LightFramework.Common.Utilities;
using Dao.LightFramework.Services.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Dao.LightFramework.Services;

public abstract class ServiceContextServiceBase : ServiceBase
{
    protected ServiceContextServiceBase(IServiceProvider serviceProvider) : base(serviceProvider) { }

    protected IRequestContext RequestContext => _<IRequestContext>();

    protected IMultilingual Lang => _<IMultilingual>();

    protected ILogger Logger => _<ILogger>();

    protected HttpContext HttpContext => _<IHttpContextAccessor>(false)?.HttpContext;

    protected string GetLang(string subKey, params object[] args) => Lang.Get(new[] { ServiceName, subKey }, args);

    protected static string NextSequentialGuid() => NewGuid.NextSequential();
}
using Dao.LightFramework.Services;

namespace Dao.LightFramework.Application;

public abstract class AppService : ServiceContextServiceBase, IAppService
{
    protected AppService(IServiceProvider serviceProvider) : base(serviceProvider) { }
}
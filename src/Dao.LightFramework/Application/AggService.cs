namespace Dao.LightFramework.Application;

public abstract class AggService : AppService, IAggService
{
    protected AggService(IServiceProvider serviceProvider) : base(serviceProvider) { }
}
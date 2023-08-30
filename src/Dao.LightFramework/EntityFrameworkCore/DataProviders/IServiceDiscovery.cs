namespace Dao.LightFramework.EntityFrameworkCore.DataProviders;

public interface IServiceDiscovery
{
    Task<string> FindService(string serviceName, int maxRetry = 0);
}
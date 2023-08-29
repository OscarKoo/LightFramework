namespace Dao.LightFramework.EntityFrameworkCore.DataProviders;

public interface IWebApiRepository<TDto> : IRepository { }

public interface IConsul
{
    Task<string> FindService(string serviceName, int maxRetry = 0);
}
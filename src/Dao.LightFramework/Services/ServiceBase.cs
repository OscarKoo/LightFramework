using Dao.LightFramework.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Dao.LightFramework.Services;

public abstract class ServiceBase : IDisposable
{
    protected string ServiceName { get; }
    protected IServiceProvider ServiceProvider { get; }

    protected ServiceBase(IServiceProvider serviceProvider)
    {
        ServiceName = GetType().Name;
        ServiceProvider = serviceProvider;
    }

    readonly Dictionary<Type, object> services = new();

    protected TIService _<TIService>(bool required = true) => (TIService)this.services.GetOrAdd(typeof(TIService),
        k => required
            ? ServiceProvider.GetRequiredService<TIService>()
            : ServiceProvider.GetService<TIService>());

    #region IDisposable

    protected virtual void Disposing()
    {
        this.services?.Clear();
    }

    public void Dispose()
    {
        Disposing();
        GC.SuppressFinalize(this);
    }

    ~ServiceBase()
    {
        Disposing();
    }

    #endregion
}
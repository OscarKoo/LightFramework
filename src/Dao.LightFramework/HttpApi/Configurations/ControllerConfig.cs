using Dao.LightFramework.HttpApi.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Dao.LightFramework.HttpApi.Configurations;

public static class ControllerConfig
{
    public static IMvcBuilder AddLightControllers(this IServiceCollection services, Action<MvcOptions> configure = null)
    {
        return services.AddControllers(o =>
        {
            o.Filters.Add<ExceptionHandler>();
            o.Filters.Add<AsyncHandlerFilter>();

            configure?.Invoke(o);
        });
    }
}
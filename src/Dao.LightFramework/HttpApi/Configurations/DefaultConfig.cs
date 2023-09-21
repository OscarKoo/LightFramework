using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Dao.LightFramework.HttpApi.Configurations;

public static class DefaultConfig
{
    public static IServiceCollection AddLightDefaultConfig(this IServiceCollection services, Action<MvcOptions> controllerConfigure = null)
    {
        services.AddLightStaticLogger();
        services.AddLightResponseCompression();
        services.AddHttpContextAccessor();
        services.AddLightControllers(controllerConfigure).AddLightJson();
        services.AddLightSwagger();
        services.AddLightHttpClient();
        return services;
    }
}
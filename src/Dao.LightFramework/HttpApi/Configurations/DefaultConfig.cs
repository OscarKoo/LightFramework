using Microsoft.Extensions.DependencyInjection;

namespace Dao.LightFramework.HttpApi.Configurations;

public static class DefaultConfig
{
    public static IServiceCollection AddLightDefaultConfig(this IServiceCollection services)
    {
        services.AddLightStaticLogger();
        services.AddLightResponseCompression();
        services.AddHttpContextAccessor();
        services.AddLightControllers().AddLightJson();
        services.AddLightSwagger();
        services.AddLightHttpClient();
        return services;
    }
}
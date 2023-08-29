using Dao.LightFramework.HttpApi.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Dao.LightFramework.HttpApi.Configurations;

public static class SwaggerConfig
{
    public static IServiceCollection AddLightSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(g => g.SchemaFilter<SwaggerIgnoreFilter>());
        return services;
    }
}
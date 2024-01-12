using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Dao.LightFramework.HttpApi.Configurations;

public static class DefaultConfig
{
    public static IServiceCollection AddLightDefaultConfig(this IServiceCollection services,
        Action<MvcOptions> controllerConfigure = null,
        CompressionLevel? compressionLevel = CompressionLevel.Optimal,
        params string[] compressionMimeTypes)
    {
        services.AddLightStaticLogger();
        if (compressionLevel != null)
            services.AddLightResponseCompression(compressionLevel.Value, compressionMimeTypes);
        services.AddHttpContextAccessor();
        services.AddLightControllers(controllerConfigure).AddLightJson();
        services.AddLightSwagger();
        services.AddLightHttpClient();
        return services;
    }
}
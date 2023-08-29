using Dao.LightFramework.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Dao.LightFramework.HttpApi.Configurations;

public static class LogConfig
{
    public static IHostBuilder AddLightSerilog(this IHostBuilder host)
    {
        host.UseSerilog((ctx, config) => config.ReadFrom.Configuration(ctx.Configuration));
        return host;
    }

    public static IServiceCollection AddLightStaticLogger(this IServiceCollection services)
    {
        var svc = services.BuildServiceProvider();
        StaticLogger.Logger = svc.GetService<ILogger<object>>();
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger), StaticLogger.Logger);
        return services;
    }
}
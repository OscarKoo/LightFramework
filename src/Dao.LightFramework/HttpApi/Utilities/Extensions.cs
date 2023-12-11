using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dao.LightFramework.HttpApi.Utilities;

public static class Extensions
{
    public static IServiceCollection AddConfiguration(this IServiceCollection services, Func<IConfigurationRoot, IConfigurationRoot> createConfiguration)
    {
        if (services == null || createConfiguration == null)
            return services;

        var builder = new ConfigurationBuilder();
        builder.AddJsonFile("appsettings.json", true, true);
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        builder.AddJsonFile($"appsettings.{environment}.json", true, true);
        var configuration = builder.Build();
        var newConfiguration = createConfiguration(configuration);
        if (newConfiguration == null)
            return services;

        var newConfig = new ConfigurationBuilder();
        using (var scope = services.BuildServiceProvider().CreateScope())
        {
            var config = scope.ServiceProvider.GetService<IConfiguration>();
            newConfig.AddConfiguration(config);
            newConfig.AddConfiguration(newConfiguration);
        }

        return services.AddSingleton<IConfiguration>(newConfig.Build());
    }
}
using System.Diagnostics;
using System.Reflection;
using Dao.LightFramework.Application;
using Dao.LightFramework.Common.Utilities;
using Dao.LightFramework.EntityFrameworkCore.DataProviders;
using Dao.LightFramework.Services.Contexts;
using EFCore.AutomaticMigrations;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Z.EntityFramework.Plus;

namespace Dao.LightFramework.HttpApi.Configurations;

public static class DependencyInjectionConfig
{
    public static ICollection<Assembly> AddLightDependencyInjection(this IServiceCollection services, IConfiguration configuration,
        Func<string, bool> matchedAssembly,
        Func<IConfiguration, string> getConnectionString, Assembly dbAssembly)
    {
        services.AddLightServiceContext();

        var assemblies = LoadAllAssemblies(matchedAssembly).ToList();

        services.AddLightDbContext(configuration, getConnectionString, dbAssembly);
        services.AddLightRepository(assemblies);
        services.AddLightAppService(assemblies);
        services.AddLightMapster(assemblies);
        services.AddLightBackgroundService(assemblies);

        return assemblies;
    }

    static bool IsMatchedAssembly(this string name, Func<string, bool> matchedAssembly) =>
        name != null && (matchedAssembly == null || matchedAssembly(name) || name.StartsWith("Dao.LightFramework", StringComparison.OrdinalIgnoreCase));

    public static IEnumerable<Assembly> LoadAllAssemblies(Func<string, bool> matchedAssembly)
    {
        foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "*.dll")
            .Where(w => Path.GetFileName(w).IsMatchedAssembly(matchedAssembly))
            .Except(AppDomain.CurrentDomain.GetAssemblies().Where(w => w.FullName.IsMatchedAssembly(matchedAssembly)).Select(s => s.Location)))
        {
            try
            {
                var name = AssemblyName.GetAssemblyName(file);
                AppDomain.CurrentDomain.Load(name);
                Trace.WriteLine($"====== Load Assembly [{name.Name}] ======");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        return AppDomain.CurrentDomain.GetAssemblies().Where(w => w.FullName.IsMatchedAssembly(matchedAssembly));
    }

    public static void RegisterAll(this IServiceCollection services, Type source, ICollection<Assembly> assemblies, bool excludeSelf, Action<Type, Type> action = null)
    {
        foreach (var iReg in assemblies.SelectMany(s => source.FindImplementations(source.IsInterface, s)).Where(w => !excludeSelf || w != source))
        {
            foreach (var assembly in assemblies)
            {
                foreach (var imp in iReg.FindImplementations(false, assembly))
                {
                    if (iReg.IsGenericType == imp.IsGenericType)
                    {
                        if (action == null)
                        {
                            Trace.WriteLine($"====== Register [{iReg.Name}] for [{imp.Name}] ======");
                            services.AddScoped(iReg, imp);
                        }
                        else
                        {
                            Trace.WriteLine($"====== Find [{iReg.Name}] & [{imp.Name}] for custom action ======");
                            action(iReg, imp);
                        }
                    }
                }
            }
        }
    }

    internal static IEnumerable<Type> FindImplementations(this Type source, bool findInterface, Assembly assembly = null)
    {
        if (source.IsGenericType)
            source = source.GetGenericTypeDefinition();
        foreach (var t in (assembly ?? source.Assembly).GetTypes().Where(w => w.IsAbstract != w.IsClass && w.IsInterface == findInterface))
        {
            var type = t;
            do
            {
                if (type.IsGenericType)
                    type = type.GetGenericTypeDefinition();

                if (source == type || source.IsAssignableFrom(type) || type.GetInterfaces().Any(w => w == source || (w.IsGenericType && w.GetGenericTypeDefinition() == source)))
                {
                    yield return t;
                    break;
                }
            } while ((type = type.BaseType) != null && type != typeof(object));
        }
    }

    public static IServiceCollection AddLightServiceContext(this IServiceCollection services)
    {
        services.AddScoped<IRequestContext, RequestContext>();
        services.AddScoped<IMultilingual, Multilingual>();
        return services;
    }

    public static IServiceCollection AddLightDbContext(this IServiceCollection services, IConfiguration configuration, Func<IConfiguration, string> getConnectionString, Assembly dbAssembly = null)
    {
        if (getConnectionString == null)
            return services;

        var connectionString = getConnectionString(configuration);
        EFContext.ConnectionString = connectionString;
        EFContext.ConfigAssembly = dbAssembly;
        services.AddDbContext<EFContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<EFContext>();
        LinqToDBForEFTools.Initialize();
        QueryCacheManager.IsEnabled = configuration.GetSection("UseQueryCache").Value.EqualsIgnoreCase(true.ToString());
        using var scope = services.BuildServiceProvider().GetService<IServiceScopeFactory>().CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<EFContext>();
        try
        {
            context.MigrateToLatestVersion(new DbMigrationsOptions { AutomaticMigrationDataLossAllowed = true });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return services;
    }

    public static IServiceCollection AddLightRepository(this IServiceCollection services, ICollection<Assembly> assemblies)
    {
        services.RegisterAll(typeof(IRepository), assemblies, true);
        return services;
    }

    public static IServiceCollection AddLightAppService(this IServiceCollection services, ICollection<Assembly> assemblies)
    {
        services.RegisterAll(typeof(IAppService), assemblies, true);
        return services;
    }

    public static IServiceCollection AddLightBackgroundService(this IServiceCollection services, ICollection<Assembly> assemblies)
    {
        services.RegisterAll(typeof(BackgroundService), assemblies, true, (_, imp) => services.TryAddEnumerable(ServiceDescriptor.Describe(typeof(IHostedService), imp, ServiceLifetime.Singleton)));
        return services;
    }
}
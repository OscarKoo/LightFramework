﻿using System.Diagnostics;
using System.Reflection;
using Dao.LightFramework.Application;
using Dao.LightFramework.Common.Utilities;
using Dao.LightFramework.EntityFrameworkCore.DataMigration;
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
    public static ICollection<Assembly> AddLightDependencyInjection(this IServiceCollection services, IConfiguration configuration, DependencyInjectionSetting setting)
    {
        setting.CheckNull(nameof(DependencyInjectionSetting));

        if (setting.UseDefaultServiceContext)
            services.AddLightServiceContext();

        services.AddLightDbContext<EFContext>(configuration, setting.GetConnectionString, setting.DataMigrationSetting, setting.UseQueryCache);
        return services.AddLightServices(setting.MatchedAssembly);
    }

    public static ICollection<Assembly> AddLightServices(this IServiceCollection services, Func<string, bool> matchedAssembly)
    {
        var assemblies = LoadAllAssemblies(matchedAssembly).ToList();
        services.AddLightRepository(assemblies);
        services.AddLightAppService(assemblies);
        services.AddLightMapster(assemblies);
        services.AddLightBackgroundService(assemblies);
        services.AddLightOnSaveChanges(assemblies);

        return assemblies;
    }

    static bool IsMatchedAssembly(this string name, Func<string, bool> matchedAssembly) =>
        name != null && (matchedAssembly == null || matchedAssembly(name) || name.StartsWith("Dao.LightFramework", StringComparison.OrdinalIgnoreCase));

    static bool IsRegistered(this IServiceCollection services, Type type) => services.Any(w => w.ServiceType == type);

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

    public static IServiceCollection RegisterAll(this IServiceCollection services, Type source, ICollection<Assembly> assemblies, bool excludeSelf, Action<Type, Type> action = null)
    {
        foreach (var iReg in assemblies.SelectMany(s => source.FindImplementations(source.IsInterface, s)).Where(w => !excludeSelf || w != source))
        {
            foreach (var assembly in assemblies)
            {
                foreach (var imp in iReg.FindImplementations(false, assembly))
                {
                    var regType = iReg;
                    if (iReg.IsGenericType != imp.IsGenericType)
                    {
                        if (imp.IsGenericType)
                            continue;

                        regType = imp.GetInterfaces().FirstOrDefault(w => iReg.IsGenericTypeDefinitionOf(w));
                        if (regType == null)
                            continue;
                    }

                    //if (services.IsRegistered(regType))
                    //    continue;

                    if (action == null)
                    {
                        Trace.WriteLine($"====== Register [{(regType.IsGenericType ? regType.FullName : regType.Name)}] for [{(imp.IsGenericType ? imp.FullName : imp.Name)}] ======");
                        services.AddScoped(regType, imp);
                    }
                    else
                    {
                        Trace.WriteLine($"====== Find [{(regType.IsGenericType ? regType.FullName : regType.Name)}] & [{(imp.IsGenericType ? imp.FullName : imp.Name)}] for custom action ======");
                        action(regType, imp);
                    }
                }
            }
        }

        return services;
    }

    internal static IEnumerable<Type> FindImplementations(this Type source, bool findInterface, Assembly assembly = null)
    {
        //if (source.IsGenericType)
        //    source = source.GetGenericTypeDefinition();
        //foreach (var t in (assembly ?? source.Assembly).GetTypes().Where(w => w.IsAbstract != w.IsClass && w.IsInterface == findInterface))
        //{
        //    var type = t;
        //    do
        //    {
        //        if (type.IsGenericType)
        //            type = type.GetGenericTypeDefinition();

        //        if (source == type || source.IsAssignableFrom(type) || type.GetInterfaces().Any(w => w == source || source.IsGenericTypeDefinitionOf(w)))
        //        {
        //            yield return t;
        //            break;
        //        }
        //    } while ((type = type.BaseType) != null && type != typeof(object));
        //}

        return (assembly ?? source.Assembly).GetTypes()
            .Where(w => w.IsAbstract != w.IsClass && w.IsInterface == findInterface
                && (w == source || source.IsAssignableFrom(w) || w.GetInterfaces().Any(a => a == source || source.IsGenericTypeDefinitionOf(a))));
    }

    public static IServiceCollection AddLightServiceContext(this IServiceCollection services)
    {
        services.AddScoped<IRequestContext, RequestContext>();
        services.AddScoped<IMultilingual, Multilingual>();
        _ = new Multilingual(null);
        return services;
    }

    public static IServiceCollection AddLightDbContext<TContext>(this IServiceCollection services,
        IConfiguration configuration,
        Func<IConfiguration, string> getConnectionString,
        DataMigrationSetting dataMigrationSetting = null,
        bool useQueryCache = false)
        where TContext : DbContext
    {
        if (getConnectionString == null)
            return services;

        var connectionString = getConnectionString(configuration);

        void DBContextOptions(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(connectionString)
                //.LogTo(s => Trace.WriteLine(s))
                //.AddInterceptors(new LightInterceptor())
                .AddInterceptors(new EFInterceptor());
        }

        services.AddDbContext<DbContext, TContext>(DBContextOptions);
        services.AddDbContext<TContext>(DBContextOptions);
        services.AddScoped<DbContext, TContext>();
        services.AddScoped<TContext>();
        LinqToDBForEFTools.Initialize();
        QueryCacheManager.IsEnabled = useQueryCache;
        using var scope = services.BuildServiceProvider().GetService<IServiceScopeFactory>().CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<DbContext>();
        try
        {
            if (!string.IsNullOrWhiteSpace(dataMigrationSetting?.DBScriptsFolder) && !string.IsNullOrWhiteSpace(dataMigrationSetting.OnMigratingFolder))
                ScriptRunner.Run(context, dataMigrationSetting.DBScriptsFolder, dataMigrationSetting.OnMigratingFolder, dataMigrationSetting.Replacements);

            context.MigrateToLatestVersion(new DbMigrationsOptions { AutomaticMigrationDataLossAllowed = true });

            if (!string.IsNullOrWhiteSpace(dataMigrationSetting?.DBScriptsFolder) && !string.IsNullOrWhiteSpace(dataMigrationSetting.OnMigratedFolder))
                ScriptRunner.Run(context, dataMigrationSetting.DBScriptsFolder, dataMigrationSetting.OnMigratedFolder, dataMigrationSetting.Replacements);

            if (!string.IsNullOrWhiteSpace(dataMigrationSetting?.DBScriptsFolder) && !string.IsNullOrWhiteSpace(dataMigrationSetting.OneTimeExecutionFolder))
                ScriptRunner.Run(context, dataMigrationSetting.DBScriptsFolder, dataMigrationSetting.OneTimeExecutionFolder, dataMigrationSetting.Replacements,
                    ScriptRunner.ShouldExecuteOneTimeExecution, ScriptRunner.OnOneTimeExecutionExecuted);
        }
        catch (Exception e)
        {
            StaticLogger.LogError(e);
            throw;
        }

        return services;
    }

    public static IServiceCollection AddLightRepository(this IServiceCollection services, ICollection<Assembly> assemblies) =>
        services.RegisterAll(typeof(IRepository), assemblies, true);

    public static IServiceCollection AddLightAppService(this IServiceCollection services, ICollection<Assembly> assemblies) =>
        services.RegisterAll(typeof(IAppService), assemblies, true);

    public static IServiceCollection AddLightBackgroundService(this IServiceCollection services, ICollection<Assembly> assemblies) =>
        services.RegisterAll(typeof(BackgroundService), assemblies, true, (_, imp) => services.TryAddEnumerable(ServiceDescriptor.Describe(typeof(IHostedService), imp, ServiceLifetime.Singleton)));

    public static IServiceCollection AddLightOnSaveChanges(this IServiceCollection services, ICollection<Assembly> assemblies)
    {
        var isGenericInterface = (Func<Type, bool>)(t => typeof(IOnSavingEntity<>).IsGenericTypeDefinitionOf(t));
        var hasGenericInterface = (Func<Type, bool>)(t => t.GetInterfaces().Any(isGenericInterface));
        services.RegisterAll(typeof(IOnSavingEntity), assemblies, false, (iReg, imp) =>
        {
            var isGenericReg = isGenericInterface(iReg) || hasGenericInterface(iReg);
            var isGenericImp = hasGenericInterface(imp);
            if (isGenericReg != isGenericImp)
                return;

            services.AddTransient(iReg, imp);

            if (!isGenericReg)
            {
                DbContextSetting.HasOnSavingAnyEntity = true;
            }
            else
            {
                var tSaving = iReg;
                if (!isGenericInterface(iReg))
                    tSaving = iReg.GetInterfaces().First(isGenericInterface);
                var entityType = tSaving.GenericTypeArguments[0];
                DbContextSetting.SavingSpecificEntityTypes.Add(entityType);
            }
        });
        services.RegisterAll(typeof(IOnSaveChanges), assemblies, false, (iReg, imp) =>
        {
            services.AddTransient(iReg, imp);
            DbContextSetting.HasOnSaveChanges = true;
        });
        return services;
    }
}

public class DependencyInjectionSetting
{
    public bool UseDefaultServiceContext { get; set; } = true;
    public Func<IConfiguration, string> GetConnectionString { get; set; }
    public DataMigrationSetting DataMigrationSetting { get; set; }
    public Func<string, bool> MatchedAssembly { get; set; }
    public bool UseQueryCache { get; set; }
}
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Dao.LightFramework.Application;
using Dao.LightFramework.Common.Attributes;
using Dao.LightFramework.Common.Utilities;
using Dao.LightFramework.Domain.Entities;
using Dao.LightFramework.Domain.Utilities;
using Dao.LightFramework.Services.Contexts;
using Mapster;
using Microsoft.Extensions.DependencyInjection;

namespace Dao.LightFramework.HttpApi.Configurations;

public static class MapsterConfig
{
    public static IServiceCollection AddLightMapster(this IServiceCollection services, ICollection<Assembly> assemblies)
    {
        TypeAdapterConfig.GlobalSettings.AllowImplicitSourceInheritance = false;

        var checkRowVersion = (Action<IRowVersion, IRowVersion>)((s, t) =>
        {
            if (t.RowVersion == null
                || (s.RowVersion != null && t.RowVersion.SequenceEqual(s.RowVersion))
                || (s is Entity { IgnoreRowVersionCheck: true } && s.IsNewerThan(t)))
                return;

            using var scope = services.BuildServiceProvider().GetService<IServiceScopeFactory>().CreateScope();
            var lang = scope.ServiceProvider.GetRequiredService<IMultilingual>();
            t.IsDtoExpired(s, lang);
        });

        var type = typeof(DtoOfAttribute<>).GetGenericTypeDefinition();
        foreach (var dtoType in assemblies.SelectMany(s => s.GetTypes()))
        {
            foreach (var attr in dtoType.GetCustomAttributes().Where(w =>
                {
                    var g = w.GetType();
                    return g.IsGenericType && g.GetGenericTypeDefinition() == type;
                }).Select(attr => (DtoOfAttribute)attr))
            {
                Debug.WriteLine($"====== Map Dto [{dtoType.Name}] to Entity [{attr.EntityType.Name}] ======");
                var setter = TypeAdapterConfig.GlobalSettings.NewConfig(dtoType, attr.EntityType).IgnoreNullValues(true).IgnoreMutable(attr.EntityType).IgnoreDomainSite(attr.EntityType); //.IgnoreLocked(entityType);
                if (!attr.IgnoreProperties.IsNullOrEmpty())
                    setter.Ignore(attr.IgnoreProperties);

                if (typeof(IRowVersion).IsAssignableFrom(dtoType) && typeof(IRowVersion).IsAssignableFrom(attr.EntityType) && !attr.IgnoreProperties.Contains(nameof(IRowVersion.RowVersion), StringComparer.Ordinal))
                    setter.Settings.BeforeMappingFactories.Add(BeforeMapping(checkRowVersion));

                Debug.WriteLine($"====== Map Entity [{attr.EntityType.Name}] to Dto [{dtoType.Name}] ======");
                setter = TypeAdapterConfig.GlobalSettings.NewConfig(attr.EntityType, dtoType).IgnoreNullValues(true);
                if (!attr.IgnoreProperties.IsNullOrEmpty())
                    setter.Ignore(attr.IgnoreProperties);
            }
        }

        foreach (var entity in assemblies.SelectMany(s => typeof(Entity).FindImplementations(false, s)).Where(w => w.BaseType is { IsAbstract: true }))
        {
            Debug.WriteLine($"====== Map Entity [{entity.Name}] to Entity [{entity.Name}] ======");
            var setter = TypeAdapterConfig.GlobalSettings.NewConfig(entity, entity).IgnoreNullValues(true);

            if (typeof(IRowVersion).IsAssignableFrom(entity))
                setter.Settings.BeforeMappingFactories.Add(BeforeMapping(checkRowVersion));
        }

        foreach (var profile in assemblies.SelectMany(sm => typeof(IMapProfile).FindImplementations(false, sm)))
        {
            Activator.CreateInstance(profile);
        }

        TypeAdapterConfig.GlobalSettings.Compile();

        TypeAdapterConfig.GlobalSettings.SetConfig();

        return services;
    }

    static Func<CompileArgument, LambdaExpression> BeforeMapping(Action<IRowVersion, IRowVersion> action)
    {
        return arg =>
        {
            var p1 = Expression.Parameter(arg.SourceType);
            var p2 = Expression.Parameter(arg.DestinationType);
            return Expression.Lambda(Expression.Call(Expression.Constant(action, action.GetType()), "Invoke", null, p1, p2), p1, p2);
        };
    }
}
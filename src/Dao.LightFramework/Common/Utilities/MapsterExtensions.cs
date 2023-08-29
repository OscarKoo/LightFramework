using Dao.LightFramework.Domain.Entities;
using Mapster;

namespace Dao.LightFramework.Common.Utilities;

public static class MapsterExtensions
{
    static volatile TypeAdapterConfig _config;

    public static void SetConfig(this TypeAdapterConfig config)
    {
        _config = config.Clone();
        foreach (var key in _config.RuleMap.Keys)
        {
            _config.ForType(key.Source, key.Destination).IgnoreNullValues(false);
        }

        _config.Compile();
    }

    static TypeAdapterConfig SwitchConfig(bool ignoreNullValue) => ignoreNullValue || _config == null ? TypeAdapterConfig.GlobalSettings : _config;

    public static TDestination Adapt<TSource, TDestination>(this TSource source, TDestination destination, bool ignoreNullValue = true) => source.Adapt(destination, SwitchConfig(ignoreNullValue));

    public static object Adapt(this object source, object destination, Type sourceType, Type destinationType, bool ignoreNullValue = true) => source.Adapt(destination, sourceType, destinationType, SwitchConfig(ignoreNullValue));

    public static List<TResult> AdaptList<TResult>(this IEnumerable<object> source) => source.Select(s => s.Adapt<TResult>()).ToList();

    public static TSetter IgnoreMutable<TSetter>(this TSetter source, Type destType)
        where TSetter : TypeAdapterSetter
    {
        if (typeof(IMutable).IsAssignableFrom(destType))
            source.Ignore(nameof(IMutable.CreateUser), nameof(IMutable.CreateTime), nameof(IMutable.UpdateUser), nameof(IMutable.UpdateTime));
        return source;
    }

    public static TSetter IgnoreDomainSite<TSetter>(this TSetter source, Type destType)
        where TSetter : TypeAdapterSetter
    {
        if (typeof(IDomainSite).IsAssignableFrom(destType))
            source.Ignore(nameof(IDomainSite.Domain), nameof(IDomainSite.Site));
        return source;
    }
}
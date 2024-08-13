using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Dao.LightFramework.Common.Attributes;
using Dao.LightFramework.Common.Utilities;

namespace Dao.LightFramework.Domain.Entities;

#region Entity

public abstract class Entity : IId
{
    public string Id { get; set; }

    [NotMapped, JsonIgnore, Newtonsoft.Json.JsonIgnore, SwaggerIgnore]
    public bool IsNew => string.IsNullOrWhiteSpace(Id) || (this is IRowVersion rw && rw.RowVersion.IsNullOrEmpty());

    [NotMapped, JsonIgnore, Newtonsoft.Json.JsonIgnore, SwaggerIgnore]
    public bool IgnoreRowVersionCheck { get; set; }

    #region CacheKeys

    [NotMapped, JsonIgnore, Newtonsoft.Json.JsonIgnore, SwaggerIgnore]
    public virtual string[] CacheKeys => null;

    public static string[] GetCacheKeys<T>(params string[] keys)
    {
        var type = typeof(T).Name;
        if (keys.IsNullOrEmpty())
            keys = new[] { string.Empty };
        return keys.Select(s => CacheKeyJoin(type, s)).Distinct().ToArray();
    }

    protected static string[] SetCacheKeys<T>(params string[] keys)
    {
        var type = typeof(T).Name;
        return string.Empty.ToEnumerable().Concat(keys).Select(key => CacheKeyJoin(type, key)).Distinct().ToArray();
    }

    public static string CacheKeyJoin(params string[] parts) => string.Join("_", parts);

    #endregion
}

public class EntityRowVersion : Entity, IRowVersion
{
    public byte[] RowVersion { get; set; }
}

public static class RowVersionExtensions
{
    public static void SyncRowVersionFrom(this IRowVersion source, IRowVersion from)
    {
        if (source == null || from?.RowVersion == null)
            return;

        source.RowVersion ??= new byte[from.RowVersion.Length];
        from.RowVersion.CopyTo(source.RowVersion, 0);
    }
}

public abstract class EntityMutable : Entity, IMutable
{
    public string CreateUser { get; set; }
    public DateTime? CreateTime { get; set; }
    public string UpdateUser { get; set; }
    public DateTime? UpdateTime { get; set; }
}

public abstract class EntityDomainSite : Entity, IDomainSite
{
    public string Domain { get; set; }
    public string Site { get; set; }
}

public abstract class EntityMutableDomainSite : EntityMutable, IDomainSite
{
    public string Domain { get; set; }
    public string Site { get; set; }
}

#endregion

#region EntityRowVersion

public abstract class EntityRowVersionMutable : EntityRowVersion, IMutable
{
    public string CreateUser { get; set; }
    public DateTime? CreateTime { get; set; }
    public string UpdateUser { get; set; }
    public DateTime? UpdateTime { get; set; }
}

public abstract class EntityRowVersionDomainSite : EntityRowVersion, IDomainSite
{
    public string Domain { get; set; }
    public string Site { get; set; }
}

public abstract class EntityRowVersionMutableDomainSite : EntityRowVersionMutable, IDomainSite
{
    public string Domain { get; set; }
    public string Site { get; set; }
}

#endregion
namespace Dao.LightFramework.Common.Attributes;

public class DtoOfAttribute : Attribute
{
    public DtoOfAttribute(Type entityType, params string[] ignoreProperties)
    {
        EntityType = entityType;
        IgnoreProperties = ignoreProperties;
    }

    public Type EntityType { get; set; }
    public string[] IgnoreProperties { get; set; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DtoOfAttribute<TEntity> : DtoOfAttribute
{
    public DtoOfAttribute(params string[] ignoreProperties) : base(typeof(TEntity), ignoreProperties) { }
}
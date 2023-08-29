using System.Linq.Expressions;
using Dao.LightFramework.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dao.LightFramework.EntityFrameworkCore.EntityConfigurations;

public abstract class EntityConfigurationBase<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : Entity
{
    protected virtual string TableName { get; }

    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        if (!string.IsNullOrWhiteSpace(TableName))
            builder.ToTable(TableName);
        else
            builder.ToTable("t" + typeof(TEntity).Name);

        builder.HasKey(a => a.Id);
        builder.SetProperty(a => a.Id, 36, true, order: 1).HasValueGenerator<IdGenerator>().ValueGeneratedOnAdd();

        if (HasInterface<IDeleted>())
        {
            builder.SetProperty(e => ((IDeleted)e).IsDeleted, true, order: 2);
            builder.HasQueryFilter(e => ((IDeleted)e).IsDeleted == false);
        }

        if (HasInterface<IDomainSite>())
        {
            builder.SetProperty(e => ((IDomainSite)e).Domain, 36, true, order: 3);
            builder.SetProperty(e => ((IDomainSite)e).Site, 36, true, order: 4);
        }

        if (HasInterface<ILocked>())
        {
            builder.SetProperty(e => ((ILocked)e).LockedUser, 36, order: 5);
            builder.SetProperty(e => ((ILocked)e).LockedTime, order: 6);
        }

        if (HasInterface<IMutable>())
        {
            builder.SetProperty(e => ((IMutable)e).CreateUser, 36, true, order: 8);
            builder.SetProperty(e => ((IMutable)e).CreateTime, true, order: 9).HasDefaultValueSql("GetDate()").ValueGeneratedOnAdd();
            builder.SetProperty(e => ((IMutable)e).UpdateUser, 36, true, order: 10);
            builder.SetProperty(e => ((IMutable)e).UpdateTime, true, order: 11).HasDefaultValueSql("GetDate()").ValueGeneratedOnAddOrUpdate().Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Save);
        }

        if (HasInterface<IRowVersion>())
            builder.SetProperty(e => ((IRowVersion)e).RowVersion, true).IsRowVersion();

        ConfigureProperties(builder);
    }

    protected bool HasInterface<TInterface>() => typeof(TInterface).IsAssignableFrom(typeof(TEntity));

    protected abstract void ConfigureProperties(EntityTypeBuilder<TEntity> builder);
}

public static class EntityTypeBuilderExtensions
{
    public static PropertyBuilder<TProperty> SetProperty<TEntity, TProperty>(this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, TProperty>> propertyExpression,
        int? maxLength = null, bool isRequired = false, string columnType = null, bool useDateTime2 = false, int? order = null)
        where TEntity : Entity
    {
        var property = builder.Property(propertyExpression);
        if (isRequired)
            property = property.IsRequired();
        if (maxLength > 0)
            property = property.HasMaxLength(maxLength.Value);
        if (!string.IsNullOrWhiteSpace(columnType))
            property = property.HasColumnType(columnType);
        else if (typeof(TProperty) == typeof(TimeSpan))
            property = property.HasColumnType("Time(0)");
        else if (!useDateTime2 && (typeof(TProperty) == typeof(DateTime) || typeof(TProperty) == typeof(DateTime?)))
            property = property.HasColumnType("datetime");
        if (order != null)
            property.HasColumnOrder(order);
        return property;
    }

    public static PropertyBuilder<TProperty> SetProperty<TEntity, TProperty>(this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, TProperty>> propertyExpression,
        bool isRequired, string columnType = null, bool useDateTime2 = false, int? order = null)
        where TEntity : Entity =>
        builder.SetProperty(propertyExpression, null, isRequired, columnType, useDateTime2, order);

    public static IndexBuilder<TEntity> SetIndex<TEntity>(this EntityTypeBuilder<TEntity> builder, string name, Expression<Func<TEntity, object>> indexExpression, bool isUnique = false, Expression<Func<TEntity, object>> includeExpression = null)
        where TEntity : Entity
    {
        var index = builder.HasIndex(indexExpression);
        if (includeExpression != null)
            index.IncludeProperties(includeExpression);
        if (isUnique)
            index.IsUnique();
        var indexName = $"{(isUnique ? "UX" : "IX")}_{builder.Metadata.GetTableName() ?? typeof(TEntity).Name}_{name}";
        index.HasDatabaseName(indexName);
        return index;
    }

    public static IndexBuilder<TEntity> SetIndex<TEntity>(this EntityTypeBuilder<TEntity> builder, string name, Expression<Func<TEntity, object>> indexExpression, Expression<Func<TEntity, object>> includeExpression)
        where TEntity : Entity =>
        builder.SetIndex(name, indexExpression, false, includeExpression);
}
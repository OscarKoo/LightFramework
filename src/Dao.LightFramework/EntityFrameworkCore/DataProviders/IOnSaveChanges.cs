using Dao.LightFramework.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Dao.LightFramework.EntityFrameworkCore.DataProviders;

public interface IPriority
{
    int Priority { get; }
}

public interface IOnSaveChanges : IPriority
{
    Task<int> OnSave(IServiceProvider serviceProvider, DbContext context, Func<Task<int>> next);
}

public interface IOnSavingEntity : IPriority
{
    Task<int> OnSaving(IServiceProvider serviceProvider, DbContext context, EntityEntry entry, Func<Task<int>> next);
}

public interface IOnSavingEntity<TEntity> : IOnSavingEntity where TEntity : Entity { }
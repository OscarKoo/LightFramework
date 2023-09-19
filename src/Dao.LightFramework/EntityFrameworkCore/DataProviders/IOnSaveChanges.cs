using Dao.LightFramework.Domain.Entities;
using Dao.LightFramework.Services.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Dao.LightFramework.EntityFrameworkCore.DataProviders;

public interface IOnSaveChanges
{
    Task<object> OnSaving(DbContext context, IList<EntityEntry> entries, IRequestContext requestContext, IServiceProvider serviceProvider);
    Task OnSaved(DbContext context, int result, IRequestContext requestContext, IServiceProvider serviceProvider, object state);
}

public interface IOnSavingEntity
{
    Task OnSaving(DbContext context, EntityEntry entry, IRequestContext requestContext, IServiceProvider serviceProvider);
}

public interface IOnSavingEntity<TEntity> : IOnSavingEntity where TEntity : Entity { }
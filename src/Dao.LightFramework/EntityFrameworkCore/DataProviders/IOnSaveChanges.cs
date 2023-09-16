using Dao.LightFramework.Services.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Dao.LightFramework.EntityFrameworkCore.DataProviders;

public interface IOnSaveChanges
{
    Task<object> OnChanging(DbContext context, IList<EntityEntry> entries, IRequestContext requestContext, IServiceProvider serviceProvider);
    Task OnChanged(DbContext context, IServiceProvider serviceProvider, object state);
}
using System.Reflection;
using Dao.LightFramework.Common.Utilities;
using Dao.LightFramework.Domain.Entities;
using Dao.LightFramework.Domain.Utilities;
using Dao.LightFramework.Services.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Z.EntityFramework.Plus;
#if DEBUG
using System.Diagnostics;
#endif

namespace Dao.LightFramework.EntityFrameworkCore.DataProviders;

public class EFContext : DbContext
{
    #region OnModelCreating

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(ConfigAssembly ?? Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    #endregion

    //public static volatile string ConnectionString;
    public static volatile Assembly ConfigAssembly;
    protected readonly IServiceProvider serviceProvider;
    protected readonly IRequestContext requestContext;

    public EFContext(DbContextOptions<EFContext> options, IServiceProvider serviceProvider) : base(options)
    {
        //if (string.IsNullOrWhiteSpace(ConnectionString))
        //    ConnectionString = options.FindExtension<SqlServerOptionsExtension>()?.ConnectionString;

        this.serviceProvider = serviceProvider;
        this.requestContext = serviceProvider.GetRequiredService<IRequestContext>();
    }

    //public static EFContext Create(IServiceProvider serviceProvider)
    //{
    //    ConnectionString.CheckNull(nameof(ConnectionString));

    //    var optionsBuilder = new DbContextOptionsBuilder<EFContext>();
    //    optionsBuilder.UseSqlServer(ConnectionString);

    //    return new EFContext(optionsBuilder.Options, serviceProvider);
    //}

    //public string GetTableName<TEntity>()
    //    where TEntity : class
    //{
    //    var type = Model.GetEntityTypes().FirstOrDefault(w => w.ClrType == typeof(TEntity));
    //    return type?.GetAnnotation("Relational:TableName").Value?.ToString();
    //}

    public readonly EntityDtoTracker EntityDtoTracker = new();

    #region SaveChangesAsync

    //IEnumerable<string> BeforeChanges()
    //{
    //    var expiredTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    //    foreach (var entry in ChangeTracker.Entries().Where(w => w.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
    //    {
    //        if (entry.State != EntityState.Deleted) this.requestContext.FillEntity(entry.Entity);

    //        var cacheKeys = ((Entity)entry.Entity).CacheKeys;
    //        if (!cacheKeys.IsNullOrEmpty())
    //        {
    //            foreach (var cacheKey in cacheKeys!)
    //            {
    //                expiredTags.Add(cacheKey);
    //            }
    //        }
    //    }

    //    return expiredTags.ToList();
    //}

    public static volatile bool OnSaveChanges;
    public static readonly HashSet<Type> OnSavingEntities = new();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
#if DEBUG
        var sw = new StopWatch();
        sw.Start();
#endif

        var expiredTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entries = ChangeTracker.Entries().Where(w => w.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);
        if (OnSaveChanges)
            entries = entries.ToList();

        await OnSaving(expiredTags, entries);

        IOnSaveChanges onChanges = null;
        object state = null;
        if (OnSaveChanges)
        {
            onChanges = this.serviceProvider.GetRequiredService<IOnSaveChanges>();
            state = await onChanges.OnSaving(this, (IList<EntityEntry>)entries, this.requestContext, this.serviceProvider);
        }

#if DEBUG
        var cost1 = sw.Stop();
        sw.Start();
#endif

        var result = await base.SaveChangesAsync(cancellationToken);

#if DEBUG
        var cost2 = sw.Stop();
        sw.Start();
#endif

        if (OnSaveChanges)
            await onChanges!.OnSaved(this, result, this.requestContext, this.serviceProvider, state);

        if (expiredTags.Count > 0)
            Parallel.ForEach(expiredTags, tag => QueryCacheManager.ExpireTag(tag));
        this.EntityDtoTracker.MapToDtos();

#if DEBUG
        var cost3 = sw.Stop();
        Debug.WriteLine($@"================= Save Changes Start =================
   BeforeChanges cost: {cost1};
SaveChangesAsync cost: {cost2};
    AfterChanges cost: {cost3};
           Total cost: {sw.Total};
================== Save Changes End ==================");
#endif

        return result;
    }

    async Task OnSaving(ISet<string> expiredTags, IEnumerable<EntityEntry> entries)
    {
        var onSavings = new Dictionary<Type, IOnSavingEntity>();
        foreach (var entry in entries)
        {
            if (entry.State != EntityState.Deleted)
                this.requestContext.FillEntity(entry.Entity);

            var entityType = entry.Metadata.ClrType;
            if (OnSavingEntities.Contains(entityType))
            {
                var onSaving = onSavings.GetOrAdd(entityType, t => (IOnSavingEntity)this.serviceProvider.GetRequiredService(typeof(IOnSavingEntity<>).MakeGenericType(t)));
                if (onSaving != null)
                    await onSaving.OnSaving(this, entry, this.requestContext, this.serviceProvider);
            }

            var cacheKeys = ((Entity)entry.Entity).CacheKeys;
            if (cacheKeys.IsNullOrEmpty())
                continue;

            foreach (var cacheKey in cacheKeys)
            {
                expiredTags.Add(cacheKey);
            }
        }

        onSavings.Clear();
    }

    #endregion
}
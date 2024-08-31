using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Dao.LightFramework.Common.Utilities;
using Dao.LightFramework.Domain.Entities;
using Dao.LightFramework.Domain.Utilities;
using Dao.LightFramework.EntityFrameworkCore.Utilities;
using Dao.LightFramework.Services.Contexts;
using Dao.LightFramework.Traces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Z.EntityFramework.Plus;
#if DEBUG
using System.Diagnostics;
#endif

namespace Dao.LightFramework.EntityFrameworkCore.DataProviders;

public static class DbContextSetting
{
    internal static bool HasOnSaveChanges { get; set; }
    internal static bool HasOnSavingAnyEntity { get; set; }
    internal static HashSet<Type> SavingSpecificEntityTypes { get; set; } = new();
    public static Assembly ConfigurationsAssembly { get; set; }
    public static string Collation { get; set; }
}

internal static class DbContextCurrent
{
    internal class Current
    {
        internal IgnoreRowVersionMode IgnoreRowVersionOnSaving { get; set; }
    }

    static readonly AsyncLocal<Current> current = new();

    internal static IgnoreRowVersionMode IgnoreRowVersionOnSaving => current.Value?.IgnoreRowVersionOnSaving ?? IgnoreRowVersionMode.None;

    public static void Add(IgnoreRowVersionMode mode)
    {
        current.Value ??= new Current();
        current.Value.IgnoreRowVersionOnSaving |= mode;
    }

    public static void Remove(IgnoreRowVersionMode mode)
    {
        if (current.Value == null)
            return;

        current.Value.IgnoreRowVersionOnSaving &= ~mode;
    }
}

public class EFContext : DbContext
{
    #region OnModelCreating

    static readonly Regex regCollation = new(@"\s+\.UseCollation\(""[A-Za-z0-9_]+""\)", RegexOptions.Compiled);

    void ChangeCollation(ModelBuilder modelBuilder, string collation)
    {
        var connStr = this.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connStr))
            return;

        var existingCollation = SqlHelper.Exec(connStr, "SELECT collation_name FROM sys.databases WHERE database_id = DB_ID()", cmd => cmd.ExecuteScalar()?.ToString());
        if (!string.IsNullOrWhiteSpace(existingCollation))
        {
            this.UpdateSnapshot(snapshot => regCollation.Replace(snapshot, string.Empty));
            return;
        }

        if (string.IsNullOrWhiteSpace(collation))
            return;

        modelBuilder.UseCollation(collation);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ChangeCollation(modelBuilder, DbContextSetting.Collation);
        modelBuilder.ApplyConfigurationsFromAssembly(DbContextSetting.ConfigurationsAssembly ?? Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    #endregion

    //public static volatile string ConnectionString;
    //public static volatile Assembly ConfigAssembly;
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

    #region OnSaving

    static bool RequireUpdate(EntityEntry entry)
    {
        PropertyEntry version = null;
        var isModified = false;
        foreach (var property in entry.Properties)
        {
            if (property.Metadata.Name == nameof(IRowVersion.RowVersion))
            {
                version = property;
                continue;
            }

            if (!property.IsModified)
                continue;

            isModified = true;
            break;
        }

        if (isModified)
            return true;

        if (version is { IsModified: true })
            version.IsModified = false;

        if (entry.Entity is IId id)
            StaticLogger.LogWarning($"SaveChangesAsync: Ignore no updates ({entry.Entity.GetType().Name}: {id})");
        return false;
    }

    #region BuildNext

    Func<Task<int>> BuildSavingSpecificEntity(EntityEntry entry, Func<Task<int>> next)
    {
        var entityType = entry.Metadata.ClrType;
        if (!DbContextSetting.SavingSpecificEntityTypes.Contains(entityType))
            return next;

        var savingEntities = this.serviceProvider.GetServices(typeof(IOnSavingEntity<>).MakeGenericType(entityType)).Cast<IOnSavingEntity>().OrderByDescending(o => o.Priority).ToList();
        if (savingEntities.Count <= 0)
            return next;

        for (var i = savingEntities.Count - 1; i >= 0; i--)
        {
            var onSavingEntity = savingEntities[i];
            var nextFunc = next;
            next = async () => await onSavingEntity.OnSaving(this.serviceProvider, this, entry, nextFunc);
        }

        return next;
    }

    Func<Task<int>> BuildSavingAnyEntity(EntityEntry entry, Func<Task<int>> next)
    {
        if (!DbContextSetting.HasOnSavingAnyEntity)
            return next;

        var savings = this.serviceProvider.GetServices<IOnSavingEntity>().OrderByDescending(o => o.Priority).ToList();
        if (savings.Count <= 0)
            return next;

        for (var i = savings.Count - 1; i >= 0; i--)
        {
            var onSaving = savings[i];
            var nextFunc = next;
            next = async () => await onSaving.OnSaving(this.serviceProvider, this, entry, nextFunc);
        }

        return next;
    }

    Func<Task<int>> BuildSavingEntities(ISet<string> expiredTags, Func<Task<int>> next)
    {
        foreach (var entry in ChangeTracker.Entries().Where(w => w.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            if (!HasRowVersion && entry.Entity is IRowVersion)
                HasRowVersion = true;

            if (entry.State != EntityState.Deleted)
            {
                if (entry.State == EntityState.Modified && !RequireUpdate(entry))
                    continue;

                this.requestContext.FillEntity(entry.Entity);
            }

            next = BuildSavingSpecificEntity(entry, next);
            next = BuildSavingAnyEntity(entry, next);

            if (!QueryCacheManager.IsEnabled)
                continue;
            var cacheKeys = ((Entity)entry.Entity).CacheKeys;
            if (cacheKeys.IsNullOrEmpty())
                continue;

            foreach (var cacheKey in cacheKeys)
            {
                expiredTags.Add(cacheKey);
            }
        }

        return next;
    }

    Func<Task<int>> BuildSaveChanges(Func<Task<int>> next)
    {
        if (!DbContextSetting.HasOnSaveChanges)
            return next;

        var saves = this.serviceProvider.GetServices<IOnSaveChanges>().OrderByDescending(o => o.Priority).ToList();
        if (saves.Count <= 0)
            return next;

        for (var i = saves.Count - 1; i >= 0; i--)
        {
            var onSave = saves[i];
            var nextFunc = next;
            next = async () => await onSave.OnSave(this.serviceProvider, this, nextFunc);
        }

        return next;
    }

    #endregion

    #endregion

    internal bool IsSaving { get; set; }
    internal bool HasRowVersion { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        double nextCost = 0;

        async Task<int> Save()
        {
            var exec = new StopWatch();
            exec.Start();
            IsSaving = true;
            var result = await base.SaveChangesAsync(cancellationToken);
            HasRowVersion = false;
            IsSaving = false;
            exec.Stop();
            nextCost = exec.LastStopNS;
            return result;
        }

        try
        {
            var sw = new StopWatch();
            sw.Start();

            var expiredTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var next = BuildSavingEntities(expiredTags, Save);
            next = BuildSaveChanges(next);

            var result = await next();

            if (expiredTags.Count > 0)
            {
                foreach (var tag in expiredTags)
                {
                    QueryCacheManager.ExpireTag(tag);
                }

                expiredTags.Clear();
            }

            this.EntityDtoTracker.MapToDtos();
            sw.Stop();

            var sb = new StringBuilder();
            sb.AppendLine($"({TraceContext.TraceId.Value}) SaveChangesAsync: Cost {sw.Format(nextCost)}");
            sb.Append($"Around SaveChanges: Cost {sw.Format(sw.TotalNS - nextCost)}");
            StaticLogger.LogInformation(sb.ToString());

            return result;
        }
        finally
        {
            DbContextCurrent.Remove(IgnoreRowVersionMode.Once);
            HasRowVersion = false;
            IsSaving = false;
        }
    }

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

    /*
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
#if DEBUG
        var sw = new StopWatch();
        sw.Start();
#endif

        var expiredTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entries = ChangeTracker.Entries().Where(w => w.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);
        if (DbContextSetting.HasOnSaveChanges)
            entries = entries.ToList();

        await OnSaving(expiredTags, entries);

        IOnSaveChanges onChanges = null;
        object state = null;
        if (DbContextSetting.HasOnSaveChanges)
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

        if (DbContextSetting.HasOnSaveChanges)
            await onChanges!.OnSaved(this, result, this.requestContext, this.serviceProvider, state);

        if (expiredTags.Count > 0)
        {
            foreach (var tag in expiredTags)
            {
                QueryCacheManager.ExpireTag(tag);
            }

            expiredTags.Clear();
        }

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
        var onSavingEntity = !DbContextSetting.HasOnSavingEntity ? null : this.serviceProvider.GetRequiredService<IOnSavingEntity>();
        var onSavings = new Dictionary<Type, IOnSavingEntity>();
        foreach (var entry in entries)
        {
            if (entry.State != EntityState.Deleted)
                this.requestContext.FillEntity(entry.Entity);

            if (DbContextSetting.HasOnSavingEntity)
                await onSavingEntity!.OnSaving(this, entry, this.requestContext, this.serviceProvider);

            var entityType = entry.Metadata.ClrType;
            if (DbContextSetting.SavingEntityTypes.Contains(entityType))
            {
                var onSaving = onSavings.GetOrAdd(entityType, t => (IOnSavingEntity)this.serviceProvider.GetRequiredService(typeof(IOnSavingEntity<>).MakeGenericType(t)));
                if (onSaving != null)
                    await onSaving.OnSaving(this, entry, this.requestContext, this.serviceProvider);
            }

            if (!QueryCacheManager.IsEnabled)
                continue;
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
    */

    #endregion
}
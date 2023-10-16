using System.Collections.Concurrent;
using System.Data;
using System.IO.Compression;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Dao.LightFramework.Common.Utilities;
using Dao.LightFramework.Domain.Entities;
using Dao.LightFramework.Domain.Utilities;
using Dao.LightFramework.EntityFrameworkCore.DataProviders;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
using Microsoft.Extensions.Caching.Memory;
using Z.EntityFramework.Plus;

namespace Dao.LightFramework.EntityFrameworkCore.Utilities;

public static class DbExtensions
{
    public static async Task ParallelQueryAsync<TIndex, TResult>(this ICollection<ParallelDbQuery<TIndex, TResult>> source, IServiceProvider serviceProvider, int degree = 0)
    {
        serviceProvider.CheckNull(nameof(serviceProvider));

        await source.ParallelForEachAsync(async (pq, svc) => pq.Result = await pq.QueryAsync(svc), serviceProvider, degree);
    }

    public static IQueryable<TEntity> AsNoTracking<TEntity>(this IQueryable<TEntity> source, bool asNoTracking) where TEntity : class => asNoTracking ? source.AsNoTracking() : source;

    static readonly ConcurrentDictionary<Type, bool> isDomainSite = new();

    public static IQueryable<TEntity> BySite<TEntity>(this IQueryable<TEntity> source, string site)
        where TEntity : class
    {
        return site != null && isDomainSite.GetOrAdd(typeof(TEntity), k => typeof(IDomainSite).IsAssignableFrom(k))
            ? source.Where(w => ((IDomainSite)w).Site == site)
            : source;
    }

    public static IQueryable<T> ById<T>(this IQueryable<T> query, string id) where T : IId => query.Where(w => w.Id == id);

    public static IQueryable<TResult> MultipleCompute<TQuery, TResult>(this IQueryable<TQuery> query, Expression<Func<IGrouping<int, TQuery>, TResult>> selector)
    {
        return query.GroupBy(g => 1).Select(selector);
    }

    public static async Task<bool> HasBeenChangedAsync<TEntity>(this IDbRepository<TEntity> repo, string id, byte[] rowVersion) where TEntity : Entity, IRowVersion, new()
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        if (rowVersion == null)
            return true;

        var entity = await repo.DbQuery(true).Where(w => w.Id == id).Select(s => new EntityRowVersion
        {
            Id = s.Id,
            RowVersion = s.RowVersion
        }).FirstOrDefaultAsync();
        return entity.IsDtoExpired(new EntityRowVersion
        {
            Id = id,
            RowVersion = rowVersion
        });
    }

    public static string GetTableName<TEntity>(this DbContext source)
    {
        var type = source?.Model.GetEntityTypes().FirstOrDefault(w => w.ClrType == typeof(TEntity));
        return type?.GetAnnotation("Relational:TableName").Value?.ToString();
    }

    public static string GetConnectionString(this DbContext source)
    {
        var optionField = typeof(DbContext).GetField("_options", BindingFlags.NonPublic | BindingFlags.Instance);
        var option = (DbContextOptions)optionField?.GetValue(source);
        return option?.FindExtension<SqlServerOptionsExtension>()?.ConnectionString;
    }

    public static void UpdateSnapshot(this DbContext source, Func<string, string> updateFunc)
    {
        var connStr = source.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connStr) || updateFunc == null)
            return;

        SqlHelper.Exec(connStr, conn =>
        {
            string id;
            string snapshot;
            using (var cmd = new SqlCommand("select top 1 MigrationId, Snapshot from __ContextSnapshot order by CreatedDate DESC", conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return 0;

                    id = reader.GetString(0);

                    using var gzipStream = new GZipStream(reader.GetStream(1), CompressionMode.Decompress);
                    using (var sr = new StreamReader(gzipStream))
                    {
                        snapshot = sr.ReadToEnd();
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(snapshot))
                return 0;
            snapshot = updateFunc(snapshot);
            if (string.IsNullOrWhiteSpace(snapshot))
                return 0;

            using (var cmd = new SqlCommand("update __ContextSnapshot set Snapshot = @Snapshot where MigrationId = @MigrationId", conn))
            {
                cmd.Parameters.Add(new SqlParameter("@MigrationId", SqlDbType.NVarChar, 150) { Value = id });

                using (var memoryStream = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Fastest, true))
                        gzipStream.Write((ReadOnlySpan<byte>)Encoding.UTF8.GetBytes(snapshot));
                    memoryStream.Seek(0L, SeekOrigin.Begin);
                    cmd.Parameters.Add(new SqlParameter("@Snapshot", SqlDbType.VarBinary, -1) { Value = memoryStream.ToArray() });
                }

                cmd.ExecuteNonQuery();
                return 0;
            }
        });
    }

    #region FromCache

    static MemoryCacheEntryOptions TillTodayEnd => new()
    {
        AbsoluteExpiration = new DateTimeOffset(DateTime.Now.AddDays(1).Date),
        SlidingExpiration = TimeSpan.FromHours(2)
    };

    public static async Task<T> FirstOrDefaultFromCacheAsync<T>(this IQueryable<T> source, bool asNoTracking, params string[] cacheKeys) where T : class =>
        !asNoTracking || cacheKeys.IsNullOrEmpty()
            ? await source.FirstOrDefaultAsync()
            : await source.DeferredFirstOrDefault().FromCacheAsync(TillTodayEnd, cacheKeys);

    public static async Task<List<T>> ToListFromCacheAsync<T>(this IQueryable<T> source, bool asNoTracking, params string[] cacheKeys) where T : class =>
        !asNoTracking || cacheKeys.IsNullOrEmpty()
            ? await source.ToListAsync()
            : (await source.FromCacheAsync(TillTodayEnd, cacheKeys)).AsList();

    public static async Task<T> SingleFromCacheAsync<T>(this IQueryable<T> source, bool asNoTracking, params string[] cacheKeys) =>
        !asNoTracking || cacheKeys.IsNullOrEmpty()
            ? await source.SingleAsync()
            : await source.DeferredSingle().FromCacheAsync(TillTodayEnd, cacheKeys);

    public static async Task<T> SingleOrDefaultFromCacheAsync<T>(this IQueryable<T> source, bool asNoTracking, params string[] cacheKeys) =>
        !asNoTracking || cacheKeys.IsNullOrEmpty()
            ? await source.SingleOrDefaultAsync()
            : await source.DeferredSingleOrDefault().FromCacheAsync(TillTodayEnd, cacheKeys);

    public static async Task<T> MaxFromCacheAsync<T>(this IQueryable<T> source, bool asNoTracking, params string[] cacheKeys) =>
        !asNoTracking || cacheKeys.IsNullOrEmpty()
            ? await source.MaxAsync()
            : await source.DeferredMax().FromCacheAsync(TillTodayEnd, cacheKeys);

    public static async Task<bool> AnyFromCacheAsync<T>(this IQueryable<T> source, bool asNoTracking, params string[] cacheKeys) =>
        !asNoTracking || cacheKeys.IsNullOrEmpty()
            ? await source.AnyAsync()
            : await source.DeferredAny().FromCacheAsync(TillTodayEnd, cacheKeys);

    public static async Task<int> CountFromCacheAsync<T>(this IQueryable<T> source, bool asNoTracking, params string[] cacheKeys) =>
        !asNoTracking || cacheKeys.IsNullOrEmpty()
            ? await source.CountAsync()
            : await source.DeferredCount().FromCacheAsync(TillTodayEnd, cacheKeys);

    #endregion
}

public class ParallelDbQuery<TIndex, TResult>
{
    public TIndex Index { get; set; }
    public TResult Result { get; set; }
    public Func<IServiceProvider, Task<TResult>> QueryAsync { get; set; }
}
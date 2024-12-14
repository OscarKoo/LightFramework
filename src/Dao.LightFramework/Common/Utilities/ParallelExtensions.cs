using Dao.LightFramework.Traces;
using Microsoft.Extensions.DependencyInjection;

namespace Dao.LightFramework.Common.Utilities;

public static class ParallelExtensions
{
    public static async Task<T> ScopeAsync<T>(this IServiceProvider source, Func<IServiceProvider, Task<T>> scopeFunc)
    {
        if (scopeFunc == null)
            return default;

        using var scope = source?.CreateScope();
        return await scopeFunc(scope?.ServiceProvider);
    }

    public static async Task<ICollection<TResult>> ParallelForEachAsync<TSource, TResult>(this ICollection<TSource> source,
        Func<TSource, int, IServiceProvider, CancellationToken, Task<TResult>> actionAsync,
        IServiceProvider serviceProvider = null, int degree = 0, CancellationToken token = new())
    {
        if (source.IsNullOrEmpty())
            return Array.Empty<TResult>();

        TraceContext.SpanId.Degrade();

        var result = new TResult[source.Count];

        if (source.Count == 1)
            await ScopeQuery(serviceProvider, source.First(), 0, token);
        else
        {
            var isRoot = false;
            if (!ParallelConfig.AllowNestedParallelism)
            {
                var isParalleling = ParallelIndicator.IsParalleling;
                isRoot = !isParalleling;
                if (isParalleling)
                {
                    var index = 0;
                    foreach (var src in source)
                    {
                        if (index == 0)
                            TraceContext.SpanId.ContinueRenew();
                        else
                            TraceContext.SpanId.Continue();
                        await ScopeAction(serviceProvider, src, index, token);
                        index++;
                    }

                    return result;
                }
            }

            if (isRoot)
                ParallelIndicator.IsParalleling = true;

            try
            {
                var option = new ParallelOptions { CancellationToken = token };
                if (degree > 0)
                    option.MaxDegreeOfParallelism = degree;
                else if (ParallelConfig.MaxDegreeOfParallelism > 0)
                    option.MaxDegreeOfParallelism = ParallelConfig.MaxDegreeOfParallelism;

                var list = source.Select((s, i) => new Tuple<TSource, int>(s, i)).ToList();
                await Parallel.ForEachAsync(list, option, async (t, ct) => await ScopeQuery(serviceProvider, t.Item1, t.Item2, ct));
            }
            finally
            {
                if (isRoot)
                    ParallelIndicator.IsParalleling = false;
            }
        }

        return result;

        async Task ScopeQuery(IServiceProvider sp, TSource src, int index, CancellationToken ct)
        {
            TraceContext.SpanId.ContinueRenew();
            await ScopeAction(sp, src, index, ct);
        }

        async Task ScopeAction(IServiceProvider sp, TSource src, int index, CancellationToken ct) =>
            await sp.ScopeAsync(async svc => result[index] = await actionAsync(src, index, svc, ct));
    }

    public static async Task<ICollection<TResult>> ParallelForEachAsync<TSource, TResult>(this ICollection<TSource> source,
        Func<TSource, IServiceProvider, Task<TResult>> actionAsync, IServiceProvider serviceProvider = null, int degree = 0) =>
        await source.ParallelForEachAsync(async (src, i, svc, ct) => await actionAsync(src, svc), serviceProvider, degree);

    public static async Task<ICollection<TResult>> ParallelForEachAsync<TSource, TResult>(this ICollection<TSource> source, Func<TSource, Task<TResult>> actionAsync, int degree = 0) =>
        await source.ParallelForEachAsync(async (src, svc) => await actionAsync(src), null, degree);

    public static async Task ParallelForEachAsync<TSource>(this ICollection<TSource> source, Func<TSource, IServiceProvider, Task> actionAsync, IServiceProvider serviceProvider = null, int degree = 0) =>
        await source.ParallelForEachAsync(async (src, svc) =>
        {
            await actionAsync(src, svc);
            return true;
        }, serviceProvider, degree);

    public static async Task ParallelForEachAsync<TSource>(this ICollection<TSource> source, Func<TSource, Task> actionAsync, int degree = 0) =>
        await source.ParallelForEachAsync(async (src, svc) => await actionAsync(src), null, degree);
}

public class ParallelConfig
{
    public static int MaxDegreeOfParallelism { get; set; }
    public static bool AllowNestedParallelism { get; set; } = true;
}

public class ParallelIndicator : AsyncLocalProvider<ParallelIndicator>
{
    bool isParalleling;
    public static bool IsParalleling
    {
        get => Get.isParalleling;
        set => Set.isParalleling = value;
    }
}
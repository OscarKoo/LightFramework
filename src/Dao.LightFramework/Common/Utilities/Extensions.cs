using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Dao.LightFramework.Common.Utilities;

public static class Extensions
{
    #region CheckNull

    public static void CheckNull(this object source, string name)
    {
        if (source == null)
            throw new ArgumentNullException(name);
    }

    public static void CheckNull(this string source, string name)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentNullException(name);
    }

    public static void CheckNull<T>(this IEnumerable<T> source, string name)
    {
        if (source.IsNullOrEmpty())
            throw new ArgumentNullException(name);
    }

    #endregion

    #region Collection

    public static bool IsNullOrEmpty<T>(this IEnumerable<T> source) => source == null || !source.Any();

    public static List<T> AsList<T>(this IEnumerable<T> source) => source as List<T> ?? source.ToList();

    public static IEnumerable<T> ToEnumerable<T>(this T source)
    {
        yield return source;
    }

    public static bool ContainsIgnoreCase(this IEnumerable<string> source, string value) =>
        source != null && source.Contains(value, StringComparer.OrdinalIgnoreCase);

    public static IEnumerable<string> DistinctIgnoreCase(this IEnumerable<string> source) => source.Distinct(StringComparer.OrdinalIgnoreCase);

    public static IOrderedEnumerable<TSource> OrderByIgnoreCase<TSource>(this IEnumerable<TSource> source, Func<TSource, string> keySelector) =>
        source.OrderBy(keySelector, StringComparer.OrdinalIgnoreCase);

    public static IOrderedEnumerable<string> OrderByIgnoreCase(this IEnumerable<string> source) =>
        source.OrderByIgnoreCase(o => o);

    public static IEnumerable<IGrouping<string, TSource>> GroupByIgnoreCase<TSource>(this IEnumerable<TSource> source, Func<TSource, string> keySelector) =>
        source.GroupBy(keySelector, StringComparer.OrdinalIgnoreCase);

    #endregion

    #region Dictionary

    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> valueFunc)
    {
        if (dict == null)
            return default;

        if (!dict.TryGetValue(key, out var value))
        {
            var newValue = valueFunc(key);
            do
            {
                if (dict.TryAdd(key, newValue))
                    return newValue;
            } while (!dict.TryGetValue(key, out value));
        }

        return value;
    }

    public static async Task<TValue> GetOrAddAsync<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TKey, Task<TValue>> valueFuncAsync)
    {
        if (dict == null)
            return default;

        if (!dict.TryGetValue(key, out var value))
        {
            var newValue = await valueFuncAsync(key);
            do
            {
                if (dict.TryAdd(key, newValue))
                    return newValue;
            } while (!dict.TryGetValue(key, out value));
        }

        return value;
    }

    public static async Task<List<TResult>> SelectAsync<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, Task<TResult>> selector)
    {
        var result = new List<TResult>();
        if (source == null)
            return result;

        foreach (var item in source)
        {
            result.Add(await selector(item));
        }

        return result;
    }

    public static async Task<List<TSource>> WhereAsync<TSource>(this IEnumerable<TSource> source, Func<TSource, Task<bool>> predicate)
    {
        var result = new List<TSource>();
        foreach (var item in source)
        {
            if (await predicate(item))
                result.Add(item);
        }

        return result;
    }

    #endregion

    #region Parallel

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

        var result = new TResult[source.Count];

        async Task ScopeQuery(TSource src, int index, IServiceProvider sp, CancellationToken ct) => await sp.ScopeAsync(async svc => result[index] = await actionAsync(src, index, svc, ct));

        if (source.Count == 1)
            await ScopeQuery(source.First(), 0, serviceProvider, token);
        else
        {
            var option = new ParallelOptions { CancellationToken = token };
            if (degree > 0)
                option.MaxDegreeOfParallelism = degree;

            var list = source.Select((s, i) => new Tuple<TSource, int>(s, i)).ToList();
            await Parallel.ForEachAsync(list, option, async (t, ct) => await ScopeQuery(t.Item1, t.Item2, serviceProvider, ct));
        }

        return result;
    }

    public static async Task<ICollection<TResult>> ParallelForEachAsync<TSource, TResult>(this ICollection<TSource> source,
        Func<TSource, IServiceProvider, Task<TResult>> actionAsync, IServiceProvider serviceProvider = null, int degree = 0)
    {
        return await source.ParallelForEachAsync(async (src, i, svc, ct) => await actionAsync(src, svc), serviceProvider, degree);
    }

    public static async Task<ICollection<TResult>> ParallelForEachAsync<TSource, TResult>(this ICollection<TSource> source,
        Func<TSource, Task<TResult>> actionAsync, int degree = 0)
    {
        return await source.ParallelForEachAsync(async (src, svc) => await actionAsync(src), null, degree);
    }

    public static async Task ParallelForEachAsync<TSource>(this ICollection<TSource> source, Func<TSource, IServiceProvider, Task> actionAsync,
        IServiceProvider serviceProvider = null, int degree = 0)
    {
        await source.ParallelForEachAsync(async (src, svc) =>
        {
            await actionAsync(src, svc);
            return true;
        }, serviceProvider, degree);
    }

    public static async Task ParallelForEachAsync<TSource>(this ICollection<TSource> source, Func<TSource, Task> actionAsync, int degree = 0)
    {
        await source.ParallelForEachAsync(async (src, svc) => await actionAsync(src), null, degree);
    }

    #endregion

    #region Reflection

    public static bool HasAttribute<T>(this MemberInfo type) where T : Attribute => type.GetCustomAttribute<T>() != null;

    public static bool HasInterface<TInterface>(this object source) => source is TInterface;

    #endregion

    #region String

    public static bool EqualsIgnoreCase(this string source, string target, bool isNullEmptySame = false) =>
        (isNullEmptySame && string.IsNullOrWhiteSpace(source) && string.IsNullOrWhiteSpace(target))
        || string.Equals(source, target, StringComparison.OrdinalIgnoreCase);

    public static string JoinUri(this string uri, params string[] parts) => (uri ?? string.Empty).ToEnumerable().Concat(parts).JoinUri();

    public static string JoinUri(this IEnumerable<string> uris) => string.Join("/", uris.Where(w => !string.IsNullOrWhiteSpace(w)).Select(s => s.Trim('/')));

    public static string ToSafeSql(this string source)
    {
        if (string.IsNullOrWhiteSpace(source)
            || !source.Contains('\'', StringComparison.Ordinal)
            || !source.Replace("''", string.Empty, StringComparison.Ordinal).Contains('\'', StringComparison.Ordinal))
            return source;

        var array = source.Split("''");
        for (var i = 0; i < array.Length; i++)
        {
            var line = array[i];
            if (line.Contains('\'', StringComparison.Ordinal))
                array[i] = line.Replace("'", "''", StringComparison.Ordinal);
        }

        return array.Length == 1
            ? array[0]
            : string.Join("''", array);
    }

    public static int ToInt32(this string source, int defaultValue = 0) =>
        string.IsNullOrWhiteSpace(source) || !int.TryParse(source, out var number)
            ? defaultValue
            : number;

    public static bool ToBool(this string source) =>
        !string.IsNullOrWhiteSpace(source)
        && (source.EqualsIgnoreCase("true")
            || source.EqualsIgnoreCase("1")
            || source.EqualsIgnoreCase("yes")
            || source.EqualsIgnoreCase("y"));

    #endregion

    #region Json

    public static string ToJson(this object source) => source == null ? null : JsonConvert.SerializeObject(source);

    public static T ToObject<T>(this string source) => source == null ? default : JsonConvert.DeserializeObject<T>(source);

    public static T JsonCopy<T>(this object source) => source == null ? default : source.ToJson().ToObject<T>();

    public static dynamic JsonCopy(this object source) => source == null ? default : JsonConvert.DeserializeObject(source.ToJson());

    #endregion

    #region DateTime

    public static int? GetAge(this DateTime? source, DateTime? now) =>
        source == null || now == null || now < source
            ? null
            : now.Value.Year - source.Value.Year - 1 +
            (now.Value.Month > source.Value.Month || (now.Value.Month == source.Value.Month && now.Value.Day >= source.Value.Day)
                ? 1
                : 0);

    public static TimeSpan ToTimeSpan(this string source, TimeSpan defaultValue = new()) =>
        string.IsNullOrWhiteSpace(source)
        || (!TimeSpan.TryParseExact(source, @"hh\:mm", null, out var result) && !TimeSpan.TryParseExact(source, @"hh\:mm\:ss", null, out result))
            ? defaultValue
            : result;

    public static string ToDateString(this DateTime? source) => source?.ToDateString();
    public static string ToDateString(this DateTime source) => source.ToString("yyyy-MM-dd");

    #endregion
}
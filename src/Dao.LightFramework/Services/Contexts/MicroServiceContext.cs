using System.Collections.Concurrent;
using Dao.IndividualLock;
using Dao.LightFramework.Common.Utilities;

namespace Dao.LightFramework.Services.Contexts;

public class MicroServiceContext : AsyncLocalProvider<MicroServiceContext>
{
    static readonly IndividualLocks<string> cacheLock = new(StringComparer.OrdinalIgnoreCase);
    readonly ConcurrentDictionary<string, object> cache = new();

    public static void CreateScopedCache(bool throwException = true)
    {
        if (Value != null)
        {
            if (throwException)
                throw new Exception("Cache can only be created once in the scoped context.");
            return;
        }

        Value = new MicroServiceContext();
    }

    public static bool TryGetCacheValue<T>(string key, out T value)
    {
        ArgumentNullException.ThrowIfNull(key);

        var cache = Value?.cache;
        if (cache == null || !cache.TryGetValue(key, out var obj))
        {
            value = default;
            return false;
        }

        value = (T)obj;
        return true;
    }

    public static T GetCacheValue<T>(string key)
    {
        TryGetCacheValue(key, out T value);
        return value;
    }

    public static async Task<T> GetCacheOrQueryAsync<T>(string key, Func<Task<T>> query)
    {
        var cache = Value?.cache;
        if (cache == null)
            return await query();

        if (TryGetCacheValue(key, out T value))
            return value;

        using (await cacheLock.LockAsync(key))
        {
            if (TryGetCacheValue(key, out value))
                return value;

            value = await query();
            cache[key] = value;
            return value;
        }
    }
}
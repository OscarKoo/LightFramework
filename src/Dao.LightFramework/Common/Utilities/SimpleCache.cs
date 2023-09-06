using System.Collections.Concurrent;
using Dao.IndividualLock;

namespace Dao.LightFramework.Common.Utilities;

public class SimpleCache<TCategory>
{
    readonly ConcurrentDictionary<string, SimpleCacheItem> cache = new(StringComparer.OrdinalIgnoreCase);
    readonly IndividualLocks<string> locks = new(StringComparer.OrdinalIgnoreCase);

    public bool TryGetValue<T>(string key, out T value)
    {
        if (!this.cache.TryGetValue(key, out var item)
            || item.Expiration <= DateTime.UtcNow)
        {
            value = default;
            return false;
        }

        value = (T)item.Value;
        return true;
    }

    public async Task<T> GetOrAddAsync<T>(string key, Func<string, Task<T>> valueFunc, int expiredMinutes = 1)
    {
        if (TryGetValue(key, out T value))
            return value;

        using (await this.locks.LockAsync(key))
        {
            if (TryGetValue(key, out value))
                return value;

            try
            {
                value = await valueFunc(key);
            }
            catch (Exception ex)
            {
                StaticLogger.LogError(ex);
                return value;
            }

            var item = new SimpleCacheItem
            {
                Value = value,
                Expiration = expiredMinutes >= 0 ? DateTime.UtcNow.AddMinutes(expiredMinutes) : DateTime.MaxValue
            };

            this.cache[key] = item;
            return value;
        }
    }
}

class SimpleCacheItem
{
    internal object Value { get; set; }
    internal DateTime Expiration { get; set; }
}
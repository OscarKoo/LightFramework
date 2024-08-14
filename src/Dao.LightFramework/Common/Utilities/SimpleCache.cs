using System.Collections.Concurrent;
using Dao.IndividualLock;

namespace Dao.LightFramework.Common.Utilities;

public class SimpleCache<TKey>
{
    readonly ConcurrentDictionary<TKey, SimpleCacheItem> cache;
    readonly IndividualLocks<TKey> locks;

    public SimpleCache(IEqualityComparer<TKey> comparer = null)
    {
        comparer ??= EqualityComparer<TKey>.Default;
        this.cache = new ConcurrentDictionary<TKey, SimpleCacheItem>(comparer);
        this.locks = new IndividualLocks<TKey>(comparer);
    }

    public bool TryGetValue<T>(TKey key, out T value)
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

    public async Task<T> GetOrAddAsync<T>(TKey key, Func<TKey, Task<T>> valueFunc, int expiredMinutes = 1)
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
                StaticLogger.LogError(ex, "[SimpleCache]");
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

    public bool Remove(TKey key) => this.cache.TryRemove(key, out _);

    public void Clear() => this.cache.Clear();
}

class SimpleCacheItem
{
    internal object Value { get; set; }
    internal DateTime Expiration { get; set; }
}
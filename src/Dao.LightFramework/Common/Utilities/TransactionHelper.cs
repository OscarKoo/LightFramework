using System.Transactions;
using Z.EntityFramework.Plus;

namespace Dao.LightFramework.Common.Utilities;

public static class TransactionHelper
{
    public static TransactionScope Create(
        TransactionScopeOption scopeOption = TransactionScopeOption.Required,
        TransactionScopeAsyncFlowOption asyncFlowOption = TransactionScopeAsyncFlowOption.Suppress)
    {
        TransactionQueryCacheTags.Initialize();
        return new TransactionScope(scopeOption, new TransactionOptions
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TransactionManager.MaximumTimeout
        }, asyncFlowOption);
    }

    #region sync

    public static T Scope<T>(Func<bool> requireTransaction, Func<T> func, TransactionScopeOption scopeOption = TransactionScopeOption.Required)
    {
        ArgumentNullException.ThrowIfNull(func);

        T result;
        using (var scope = requireTransaction == null || requireTransaction() ? Create(scopeOption) : null)
        {
            result = func();
            scope?.Complete();
        }
        TransactionQueryCacheTags.ExpireTags();
        return result;
    }

    public static T Scope<T>(Func<T> func, TransactionScopeOption scopeOption = TransactionScopeOption.Required) => Scope(null, func, scopeOption);

    public static void Scope(Func<bool> requireTransaction, Action action, TransactionScopeOption scopeOption = TransactionScopeOption.Required)
    {
        ArgumentNullException.ThrowIfNull(action);

        Scope(requireTransaction, () =>
        {
            action();
            return true;
        }, scopeOption);
    }

    public static void Scope(Action action, TransactionScopeOption scopeOption = TransactionScopeOption.Required) => Scope(null, action, scopeOption);

    #endregion

    #region async

    public static async Task<T> ScopeAsync<T>(Func<bool> requireTransaction, Func<Task<T>> funcAsync, TransactionScopeOption scopeOption = TransactionScopeOption.Required)
    {
        ArgumentNullException.ThrowIfNull(funcAsync);

        T result;
        using (var scope = requireTransaction == null || requireTransaction() ? Create(scopeOption, TransactionScopeAsyncFlowOption.Enabled) : null)
        {
            result = await funcAsync();
            scope?.Complete();
        }
        TransactionQueryCacheTags.ExpireTags();
        return result;
    }

    public static async Task<T> ScopeAsync<T>(Func<Task<T>> funcAsync, TransactionScopeOption scopeOption = TransactionScopeOption.Required) => await ScopeAsync(null, funcAsync, scopeOption);

    public static async Task ScopeAsync(Func<bool> requireTransaction, Func<Task> actionAsync, TransactionScopeOption scopeOption = TransactionScopeOption.Required)
    {
        ArgumentNullException.ThrowIfNull(actionAsync);

        await ScopeAsync(requireTransaction, async () =>
        {
            await actionAsync();
            return true;
        }, scopeOption);
    }

    public static async Task ScopeAsync(Func<Task> actionAsync, TransactionScopeOption scopeOption = TransactionScopeOption.Required) => await ScopeAsync(null, actionAsync, scopeOption);

    #endregion
}

public class TransactionQueryCacheTags : AsyncLocalProvider<TransactionQueryCacheTags>
{
    ISet<string> cacheTags;

    public static void Initialize() => Set.cacheTags ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public static void Add(params string[] tags)
    {
        if (tags is not { Length: > 0 })
            return;

        Add((IEnumerable<string>)tags);
    }

    public static void Add(IEnumerable<string> tags)
    {
        var cache = Value?.cacheTags;
        if (cache == null)
            return;

        foreach (var tag in tags)
            cache.Add(tag);
    }

    public static void ExpireTags()
    {
        var cache = Value?.cacheTags;
        if (cache is not { Count: > 0 })
            return;

        foreach (var tag in cache)
        {
            QueryCacheManager.ExpireTag(tag);
        }
    }
}
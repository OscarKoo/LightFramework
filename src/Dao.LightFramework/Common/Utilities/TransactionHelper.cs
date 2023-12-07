using System.Transactions;

namespace Dao.LightFramework.Common.Utilities;

public static class TransactionHelper
{
    public static TransactionScope Create(
        TransactionScopeOption scopeOption = TransactionScopeOption.Required,
        TransactionScopeAsyncFlowOption asyncFlowOption = TransactionScopeAsyncFlowOption.Suppress) =>
        new(scopeOption, new TransactionOptions
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TransactionManager.MaximumTimeout
        }, asyncFlowOption);

    #region sync

    public static T Scope<T>(Func<bool> requireTransaction, Func<T> func, TransactionScopeOption scopeOption = TransactionScopeOption.Required)
    {
        T result = default;
        if (func == null)
            return result;

        using var scope = requireTransaction?.Invoke() ?? true ? Create(scopeOption) : null;
        result = func();
        scope?.Complete();
        return result;
    }

    public static T Scope<T>(Func<T> func, TransactionScopeOption scopeOption = TransactionScopeOption.Required) => Scope(null, func, scopeOption);

    public static void Scope(Func<bool> requireTransaction, Action action, TransactionScopeOption scopeOption = TransactionScopeOption.Required)
    {
        if (action == null)
            return;

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
        T result = default;
        if (funcAsync == null)
            return result;

        using var scope = requireTransaction?.Invoke() ?? true ? Create(scopeOption, TransactionScopeAsyncFlowOption.Enabled) : null;
        result = await funcAsync();
        scope?.Complete();
        return result;
    }

    public static async Task<T> ScopeAsync<T>(Func<Task<T>> funcAsync, TransactionScopeOption scopeOption = TransactionScopeOption.Required) => await ScopeAsync(null, funcAsync, scopeOption);

    public static async Task ScopeAsync(Func<bool> requireTransaction, Func<Task> actionAsync, TransactionScopeOption scopeOption = TransactionScopeOption.Required)
    {
        if (actionAsync == null)
            return;

        await ScopeAsync(requireTransaction, async () =>
        {
            await actionAsync();
            return true;
        }, scopeOption);
    }

    public static async Task ScopeAsync(Func<Task> actionAsync, TransactionScopeOption scopeOption = TransactionScopeOption.Required) => await ScopeAsync(null, actionAsync, scopeOption);

    #endregion
}
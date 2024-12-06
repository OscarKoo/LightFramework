#nullable enable
namespace Dao.LightFramework.Common.Utilities;

public abstract class AsyncLocalProvider<T>
    where T : new()
{
    static readonly AsyncLocal<T> local = new();

    protected static T? Value
    {
        get => local.Value;
        set => local.Value = value;
    }

    protected static T Get => Value ?? new T();
    protected static T Set => Value ??= new T();

    protected static T LockSet(Func<T> create)
    {
        var value = Value;
        if (value != null)
            return value;

        lock (local)
        {
            value = Value;
            if (value == null)
                Value = value = create();
            return value;
        }
    }
}
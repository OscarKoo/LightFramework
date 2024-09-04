namespace Dao.LightFramework.Common.Utilities;

public class RefOut<T>
{
    public RefOut()
    {
    }

    public RefOut(T value) => Value = value;

    public T Value { get; set; }

    public static implicit operator T(RefOut<T> refOut) => refOut.Value;

    public static implicit operator RefOut<T>(T value) => new(value);

    public static RefOut<T> Create(T value = default) => new(value);
}
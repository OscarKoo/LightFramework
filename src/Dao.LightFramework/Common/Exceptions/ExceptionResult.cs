namespace Dao.LightFramework.Common.Exceptions;

public class ExceptionResult
{
    public string Type { get; set; }
    public string Message { get; set; }
    public object Data { get; set; }

    public bool ShouldSerializeData() => Data != null;
}
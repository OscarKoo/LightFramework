namespace Dao.LightFramework.Common.Exceptions;

public class HttpException : Exception
{
    public int? StatusCode { get; set; }
    public new string Message { get; set; }
}
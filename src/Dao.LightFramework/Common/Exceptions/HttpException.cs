namespace Dao.LightFramework.Common.Exceptions;

public class HttpException : Exception
{
    public HttpException(string message, int statusCode) : base(message) => StatusCode = statusCode;

    public int StatusCode { get; }
}

public class HttpResultException : HttpException
{
    public HttpResultException(string message, int statusCode) : base(message, statusCode)
    {
    }
}
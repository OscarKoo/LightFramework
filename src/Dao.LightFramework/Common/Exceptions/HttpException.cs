namespace Dao.LightFramework.Common.Exceptions;

public class HttpException : Exception
{
    public HttpException(string message, int statusCode) : base(message) => StatusCode = statusCode;

    public int StatusCode { get; }
}

public class HttpResultException : HttpException
{
    public HttpResultException(string request, string error, int statusCode) : base(Parse(request, error), statusCode)
    {
        Request = request;
        Error = error;
    }

    public HttpResultException(string message, int statusCode) : base(message, statusCode) => Parse(message);

    public string Request { get; private set; }
    public string Error { get; private set; }

    static string Parse(string request, string error) => string.Join(Environment.NewLine, $"{request} Failed! ", $"Error: {error}");

    void Parse(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var msgs = message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (msgs.Length <= 0)
            return;

        var request = msgs[0];
        if (request.EndsWith(" Failed! ", StringComparison.OrdinalIgnoreCase))
            request = request[..^" Failed! ".Length];
        Request = request;

        if (msgs.Length <= 1)
            return;

        var error = msgs[1];
        if (error.StartsWith("Error: ", StringComparison.OrdinalIgnoreCase))
            msgs[1] = error["Error: ".Length..];
        Error = string.Join(Environment.NewLine, error.Skip(1));
    }
}
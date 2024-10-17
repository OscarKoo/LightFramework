namespace Dao.LightFramework.Services.Contexts;

public class RequestContextInfo
{
    static readonly AsyncLocal<RequestContextInfo> instance = new();

    static RequestContextInfo Get() => instance.Value ?? new RequestContextInfo();
    static RequestContextInfo Set()
    {
        var value = instance.Value;
        if (value != null)
            return value;

        lock (instance)
        {
            value = instance.Value;
            if (value == null)
                instance.Value = value = new RequestContextInfo();
            return value;
        }
    }

    string method;
    public static string Method
    {
        get => Get().method;
        set => Set().method = value;
    }

    string route;
    public static string Route
    {
        get => Get().route;
        set => Set().route = value;
    }

    bool noLog;
    public static bool NoLog
    {
        get => Get().noLog;
        set => Set().noLog = value;
    }

    public const string NoLog_Header = "X-No-Log";
    public const string NoLog_Query = "nolog";
}
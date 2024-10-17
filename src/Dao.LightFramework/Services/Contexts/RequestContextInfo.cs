namespace Dao.LightFramework.Services.Contexts;

public class RequestContextInfo
{
    static readonly AsyncLocal<RequestContextInfo> instance = new();

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
        get
        {
            var value = instance.Value;
            return value?.method;
        }
        set => Set().method = value;
    }

    string route;
    public static string Route
    {
        get
        {
            var value = instance.Value;
            return value?.route;
        }
        set => Set().route = value;
    }

    bool noLog;
    public static bool NoLog
    {
        get
        {
            var value = instance.Value;
            return value?.noLog ?? false;
        }
        set => Set().noLog = value;
    }

    public const string NoLog_Header = "X-No-Log";
    public const string NoLog_Query = "nolog";
}
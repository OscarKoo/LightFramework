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

    int noLog;
    public static int NoLog
    {
        get
        {
            var value = instance.Value;
            return value?.noLog ?? 0;
        }
        set => Set().noLog = value;
    }

    static readonly int[] logEnabledValues = { 0 };
    public static bool IsLogEnabled(int value) => logEnabledValues.Contains(value);
    public static bool LogEnabled => IsLogEnabled(NoLog);

    public const string NoLog_Header = "X-No-Log";
    public const string NoLog_Query = "noLog";
}
using Dao.LightFramework.Common.Utilities;

namespace Dao.LightFramework.Services.Contexts;

public class RequestContextInfo : AsyncLocalProvider<RequestContextInfo>
{
    string method;
    public static string Method
    {
        get => Get.method;
        set => Set.method = value;
    }

    string route;
    public static string Route
    {
        get => Get.route;
        set => Set.route = value;
    }

    #region NoLog

    int noLog;
    public static int NoLog
    {
        get => Get.noLog;
        set => Set.noLog = value;
    }

    static readonly int[] logEnabledValues = { 0 };
    public static bool IsLogEnabled(int value) => logEnabledValues.Contains(value);
    public static bool LogEnabled => IsLogEnabled(NoLog);

    public const string NoLog_Header = "X-No-Log";
    public const string NoLog_Query = "noLog";

    #endregion

    IRequestContext context;
    public static IRequestContext Context
    {
        get => Value?.context;
        set => Set.context = value;
    }
}
using Microsoft.Extensions.Logging;

namespace Dao.LightFramework.Common.Utilities;

public static class LogExtensions
{
    public static void Debug(this ILogger logger, Func<string> messageFunc)
    {
        if (logger == null || messageFunc == null || !logger.IsEnabled(LogLevel.Debug))
            return;

        logger.LogDebug(messageFunc());
    }

    public static T ExecIf<T>(this ILogger logger, LogLevel level, Func<T> execFunc) =>
        logger == null || execFunc == null || !logger.IsEnabled(level) ? default : execFunc();
}
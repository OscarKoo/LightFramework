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

    public static T RunAs<T>(this ILogger logger, Func<T> createFunc, LogLevel level = LogLevel.Debug) =>
        logger == null || createFunc == null || !logger.IsEnabled(level) ? default : createFunc();
}
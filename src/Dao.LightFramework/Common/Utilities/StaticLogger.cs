using Microsoft.Extensions.Logging;

namespace Dao.LightFramework.Common.Utilities;

public class StaticLogger
{
    public static volatile ILogger Logger;

    public static void LogInformation(string message, params object[] args) => Logger?.LogInformation(message, args);
    public static void LogWarning(string message, params object[] args) => Logger?.LogWarning(message, args);
    public static void LogError(string message, params object[] args) => Logger?.LogError(message, args);
    public static void LogError(Exception ex, string message = "", params object[] args) => Logger?.LogError(ex, message, args);
    public static void LogDebug(string message, params object[] args) => Logger?.LogDebug(message, args);
    public static void LogDebug(Func<string> messageFunc) => Logger.Debug(messageFunc);
    public static T ExecIf<T>(LogLevel level, Func<T> execFunc) => Logger.ExecIf(level, execFunc);
}
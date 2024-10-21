using Dao.LightFramework.Services.Contexts;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Dao.LightFramework.Common.Utilities;

public class SeriLoggerSetting
{
    public static Func<string, ILogger> CreateLogger { get; set; }
    public static ILogEventFilter[] Filters { get; set; }

    public static LogEventLevel LogLevel { get; set; } = LogEventLevel.Verbose;
    public static string OutputTemplate { get; set; } = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
    public static long? FileSizeLimitBytes { get; set; } = 1073741824;
    public static RollingInterval RollingInterval { get; set; } = RollingInterval.Hour;
    public static int? RetainedFileCountLimit { get; set; } = 168;
    public static TimeSpan? RetainedFileTimeLimit { get; set; }

    public static LoggerConfiguration CreateDefaultConfiguration(string serviceName,
        LogEventLevel? logLevel = null,
        string outputTemplate = null,
        long? fileSizeLimitBytes = null,
        RollingInterval? rollingInterval = null,
        int? retainedFileCountLimit = null,
        TimeSpan? retainedFileTimeLimit = null) =>
        new LoggerConfiguration()
            .MinimumLevel.Is(logLevel ?? LogLevel)
            .Filter.With(Filters ?? DefaultFilters)
            .WriteTo.Console(LogEventLevel.Information, $"[{{Timestamp:HH:mm:ss}} {{Level:u3}}] ({serviceName}) {{Message:lj}}{{NewLine}}")
            .WriteTo.File($"./Logs/{serviceName.ToLowerInvariant()}_log_.txt",
                logLevel ?? LogLevel,
                outputTemplate ?? OutputTemplate,
                fileSizeLimitBytes: fileSizeLimitBytes ?? FileSizeLimitBytes,
                shared: true,
                rollingInterval: rollingInterval ?? RollingInterval,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: retainedFileCountLimit ?? RetainedFileCountLimit,
                retainedFileTimeLimit: retainedFileTimeLimit ?? RetainedFileTimeLimit);

    public static ILogger CreateDefaultLogger(string serviceName,
        LogEventLevel? logLevel = null,
        string outputTemplate = null,
        long? fileSizeLimitBytes = null,
        RollingInterval? rollingInterval = null,
        int? retainedFileCountLimit = null,
        TimeSpan? retainedFileTimeLimit = null) =>
        CreateDefaultConfiguration(serviceName, logLevel, outputTemplate, fileSizeLimitBytes, rollingInterval, retainedFileCountLimit, retainedFileTimeLimit).CreateLogger();

    public static readonly ILogEventFilter[] DefaultFilters = { new DefaultFilter() };

    public class DefaultFilter : ILogEventFilter
    {
        public bool IsEnabled(LogEvent logEvent) => RequestContextInfo.LogEnabled;
    }

    public class MethodRouteFilter : ILogEventFilter
    {
        readonly Dictionary<string, HashSet<string>> method_routes;
        readonly bool isIncluding;

        public MethodRouteFilter(bool isIncluding, params string[] method_routes)
        {
            this.isIncluding = isIncluding;
            if (!method_routes.IsNullOrEmpty())
            {
                this.method_routes = method_routes.Select(s =>
                    {
                        var pair = s.Split(":", StringSplitOptions.TrimEntries);
                        return (pair[0], pair.Length > 1 ? pair[1] : string.Empty);
                    }).GroupBy(g => g.Item1, StringComparer.OrdinalIgnoreCase)
                    .Select(s => (s.Key, new HashSet<string>(s.Select(v => v.Item2), StringComparer.OrdinalIgnoreCase)))
                    .ToDictionary(kv => kv.Key, kv => kv.Item2, StringComparer.OrdinalIgnoreCase);
            }
        }

        public bool IsEnabled(LogEvent logEvent)
        {
            var method = RequestContextInfo.Method;
            var route = RequestContextInfo.Route;

            if (this.method_routes == null || method == null || route == null)
                return true;

            var allMethods = this.method_routes.GetValueOrDefault(string.Empty);
            return this.isIncluding == ((this.method_routes.TryGetValue(method, out var routes) && routes != null && (routes.Contains(route) || routes.Contains(string.Empty)))
                || (allMethods != null && (allMethods.Contains(route) || allMethods.Contains(string.Empty))));
        }
    }
}

public class SeriLogger<TService>
{
    static readonly ILogger logger;

    static SeriLogger()
    {
        var serviceName = typeof(TService).Name;
        var instance = SeriLoggerSetting.CreateLogger != null
            ? SeriLoggerSetting.CreateLogger(serviceName)
            : SeriLoggerSetting.CreateDefaultLogger(serviceName);
        logger = instance.ForContext(Constants.SourceContextPropertyName, serviceName);
    }

    readonly string name;
    public SeriLogger(string name = null) => this.name = name;

    const string format = "[{0}]: {1}";
    string MessageTemplate(string message) => this.name == null ? message : string.Format(format, this.name, message);

    public void Debug(string message) => logger.Debug(MessageTemplate(message));
    public void Info(string message) => logger.Information(MessageTemplate(message));
    public void Warn(string message) => logger.Warning(MessageTemplate(message));
    public void Error(string message, Exception ex = null) => logger.Error(ex, MessageTemplate(message));
    public void Error(Exception ex) => Error(MessageTemplate(ex.GetBaseException().Message), ex);
}
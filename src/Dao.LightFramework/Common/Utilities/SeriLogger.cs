﻿using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Dao.LightFramework.Common.Utilities;

public class SeriLoggerSetting
{
    public static Func<string, ILogger> CreateLogger { get; set; }

    public static LoggerConfiguration CreateDefaultConfiguration(string serviceName, LogEventLevel logLevel = LogEventLevel.Verbose) =>
        new LoggerConfiguration()
            .MinimumLevel.Is(logLevel)
            .WriteTo.Console(LogEventLevel.Information, $"[{{Timestamp:HH:mm:ss}} {{Level:u3}}] ({serviceName}) {{Message:lj}}{{NewLine}}")
            .WriteTo.File($"./Logs/{serviceName.ToLowerInvariant()}_log_.txt", logLevel, shared: true, rollingInterval: RollingInterval.Hour, rollOnFileSizeLimit: true, retainedFileCountLimit: 168);

    public static ILogger CreateDefaultLogger(string serviceName, LogEventLevel logLevel = LogEventLevel.Verbose) =>
        CreateDefaultConfiguration(serviceName, logLevel).CreateLogger();
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
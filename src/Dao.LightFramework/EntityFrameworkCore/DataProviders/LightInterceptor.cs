using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using Dao.LightFramework.Common.Utilities;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dao.LightFramework.EntityFrameworkCore.DataProviders;

public class LightInterceptor : DbCommandInterceptor
{
    readonly uint attention;

    public LightInterceptor() => this.attention = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").EqualsIgnoreCase("Development") ? 100 : (uint)1000;

    string Format(double ticks)
    {
        var nano = 1000000000 * ticks / Stopwatch.Frequency;
        var ms = Math.Round(nano / 1000000, 1);
        var us = Math.Round(nano / 1000, 1);
        var ns = Math.Round(nano, 1);
        return $"{(ms > this.attention ? "[ATTENTION] " : "")}{ms} ms, {us} us, {ns} ns";
    }

    void Performance(string name, TimeSpan ts, IDbCommand command)
    {
        if (ts.TotalMilliseconds <= this.attention)
            return;

        var sb = new StringBuilder();
        sb.AppendLine($"{name}: Cost {Format(ts.Ticks)}");
        sb.Append("Slow SQL: " + command.CommandText);
        StaticLogger.LogInformation(sb.ToString());
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        Performance(nameof(NonQueryExecuted), eventData.Duration, command);
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override async ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = new())
    {
        Performance(nameof(NonQueryExecutedAsync), eventData.Duration, command);
        return await base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override object ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object result)
    {
        Performance(nameof(ScalarExecuted), eventData.Duration, command);
        return base.ScalarExecuted(command, eventData, result);
    }

    public override async ValueTask<object> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object result, CancellationToken cancellationToken = new())
    {
        Performance(nameof(ScalarExecutedAsync), eventData.Duration, command);
        return await base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        Performance(nameof(ReaderExecuted), eventData.Duration, command);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = new())
    {
        Performance(nameof(ReaderExecutedAsync), eventData.Duration, command);
        return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
}
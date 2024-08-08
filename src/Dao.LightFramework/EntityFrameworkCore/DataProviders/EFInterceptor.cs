using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dao.LightFramework.EntityFrameworkCore.DataProviders;

public class EFInterceptor : DbCommandInterceptor
{
    static readonly Regex regRowVersion = new(@" AND \[RowVersion\] = @[^ @;]+", RegexOptions.Compiled);

    static void OnSaving(DbCommand command, CommandEventData eventData)
    {
        if (eventData.CommandSource != CommandSource.SaveChanges
            || eventData.Context is not EFContext { IsSaving: true, HasRowVersion: true })
            return;

        var ignore = DbContextCurrent.IgnoreRowVersionOnSaving;
        if ((int)ignore <= 0 || ignore.HasFlag(IgnoreRowVersionMode.Never))
            return;

        var sql = command.CommandText;
        command.CommandText = regRowVersion.Replace(sql, "");
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        OnSaving(command, eventData);
        return base.ReaderExecuting(command, eventData, result);
    }


    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = new())
    {
        OnSaving(command, eventData);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }
}
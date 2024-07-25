using Microsoft.EntityFrameworkCore;

namespace Dao.LightFramework.Common.Exceptions;

public class DataHasChangedException : WarningException
{
    public DataHasChangedException(string message, object entity = null, object dto = null) : base(message)
    {
        Entity = entity;
        Dto = dto;
    }

    public object Entity { get; }
    public object Dto { get; }
}

public static class ExceptionExtensions
{
    public static void ThrowDataHasChangedException(this DbUpdateConcurrencyException ex, Func<string> funcMessage, object dto = null)
    {
        foreach (var entry in ex.Entries)
        {
            throw new DataHasChangedException(funcMessage(), entry.Entity, dto);
        }
    }
}
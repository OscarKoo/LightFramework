namespace Dao.LightFramework.Common.Exceptions;

public class DataHasChangedException : WarningException
{
    public DataHasChangedException(string message) : base(message) { }
}
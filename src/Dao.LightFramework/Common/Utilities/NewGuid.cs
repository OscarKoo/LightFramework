using MassTransit;

namespace Dao.LightFramework.Common.Utilities;

public static class NewGuid
{
    public static string NextSequential() => NewId.NextSequentialGuid().ToString();
}
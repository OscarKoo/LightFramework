using System.Data;
using Microsoft.Data.SqlClient;

namespace Dao.LightFramework.Common.Utilities;

class SqlHelper
{
    public static TResult Exec<TResult>(string connectionString, Func<SqlConnection, TResult> execFunc)
    {
        connectionString.CheckNull(nameof(connectionString));
        execFunc.CheckNull(nameof(execFunc));

        using var conn = new SqlConnection(connectionString);
        try
        {
            return execFunc(conn);
        }
        catch (Exception ex)
        {
            return default;
        }
    }

    public static TResult Exec<TResult>(string connectionString, string cmdText, Func<SqlCommand, TResult> execFunc, CommandType commandType = CommandType.Text)
    {
        cmdText.CheckNull(nameof(cmdText));

        return Exec(connectionString, conn =>
        {
            using var cmd = new SqlCommand(cmdText, conn) { CommandType = commandType };
            conn.Open();
            return execFunc(cmd);
        });
    }
}
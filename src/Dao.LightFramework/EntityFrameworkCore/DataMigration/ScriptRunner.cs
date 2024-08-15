using Dao.LightFramework.Common.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace Dao.LightFramework.EntityFrameworkCore.DataMigration;

public static class ScriptRunner
{
    public static void Run(DbContext context, string parentFolder, string folder, Dictionary<string, string> replacements, Func<string, ServerConnection, bool> shouldExecute = null, Action<bool, string, ServerConnection> onExecuted = null)
    {
        var dir = $"{parentFolder}\\{folder}";
        StaticLogger.LogInformation($"ScriptRunner reading \"{dir}\" scripts...");

        var root = AppDomain.CurrentDomain.BaseDirectory;
        var path = Path.Combine(root, parentFolder, folder);
        if (!Directory.Exists(path))
        {
            StaticLogger.LogInformation($"\"{dir}\" does not exist! ScriptRunner exited.");
            return;
        }

        foreach (var file in Directory.EnumerateFiles(path, "*.sql").Select(s => new FileInfo(s)).OrderBy(o =>
        {
            var index = o.Name.IndexOf(".", StringComparison.Ordinal);
            return index >= 0 ? o.Name[..index] : int.MaxValue.ToString();
        }).ThenBy(o => o.Name))
        {
            var fileName = file.Name;
            try
            {
                var script = File.ReadAllText(file.FullName);
                if (!replacements.IsNullOrEmpty())
                    script = replacements.Aggregate(script, (current, kv) => current.Replace(kv.Key, kv.Value));

                //context.Database.ExecuteSqlRaw(script);

                using (var sqlConn = new SqlConnection(context.Database.GetConnectionString()))
                {
                    var sqlContext = new Server(new ServerConnection(sqlConn)).ConnectionContext;
                    sqlContext.StatementTimeout = 0;

                    Initialize(sqlContext);

                    var shouldExec = shouldExecute == null || shouldExecute(fileName, sqlContext);
                    if (shouldExec)
                        sqlContext.ExecuteNonQuery(script);

                    onExecuted?.Invoke(shouldExec, fileName, sqlContext);
                }

                var newFile = Path.Combine(root, parentFolder, "Finished", folder, fileName);
                File2.Move(file.FullName, newFile);
            }
            catch (Exception ex)
            {
                throw new Exception($"ScriptRunner failed! File: \"{dir}\\{fileName}\" Error: {ex.GetBaseException().Message}");
            }
        }

        StaticLogger.LogInformation($"ScriptRunner executed \"{dir}\" scripts.");
    }

    static void Initialize(ServerConnection sqlContext)
    {
        var sql = @"
IF OBJECT_ID('__OneTimeExecution', 'U') IS NULL
BEGIN
    CREATE TABLE __OneTimeExecution (
        Id INT IDENTITY(1, 1) CONSTRAINT PK___OneTimeExecution PRIMARY KEY,
        ScriptName NVARCHAR(128) NOT NULL,
        ExecutionTime DATETIME NOT NULL
    )
END
";
        sqlContext.ExecuteNonQuery(sql);
    }

    public static bool ShouldExecuteOneTimeExecution(string fileName, ServerConnection sqlContext)
    {
        using var cmd = new SqlCommand("SELECT TOP 1 * FROM dbo.__OneTimeExecution WHERE ScriptName = @ScriptName ORDER BY Id DESC");
        cmd.Connection = sqlContext.SqlConnectionObject;
        cmd.CommandTimeout = 0;
        cmd.Parameters.Add(new SqlParameter("@ScriptName", fileName));
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            var lastScriptName = reader.GetString(1);
            var lastExecutionTime = reader.GetDateTime(2);
            if (lastScriptName == fileName)
            {
                StaticLogger.LogInformation($"Ignored OneTimeExecution script \"{fileName}\", Last executed time: {lastExecutionTime:yyyy-MM-dd HH:mm:ss}");
                return false;
            }
        }

        return true;
    }

    public static void OnOneTimeExecutionExecuted(bool shouldExecuting, string fileName, ServerConnection sqlContext)
    {
        if (!shouldExecuting)
            return;

        using var cmd = new SqlCommand("INSERT INTO dbo.__OneTimeExecution (ScriptName, ExecutionTime) VALUES (@ScriptName, @ExecutionTime)");
        cmd.Connection = sqlContext.SqlConnectionObject;
        cmd.CommandTimeout = 0;
        cmd.Parameters.Add(new SqlParameter("@ScriptName", fileName));
        cmd.Parameters.Add(new SqlParameter("@ExecutionTime", DateTime.Now));
        cmd.ExecuteNonQuery();

        StaticLogger.LogInformation($"OneTimeExecution script \"{fileName}\" executed.");
    }
}
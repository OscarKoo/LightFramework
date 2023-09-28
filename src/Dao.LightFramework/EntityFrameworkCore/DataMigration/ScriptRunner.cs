using Dao.LightFramework.Common.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace Dao.LightFramework.EntityFrameworkCore.DataMigration;

public static class ScriptRunner
{
    public static void Run(DbContext context, string parentFolder, string folder, Dictionary<string, string> replacements)
    {
        var dir = $"{parentFolder}\\{folder}";
        StaticLogger.LogInformation($"ScriptRunner reading \"{dir}\" scripts...");

        var root = AppDomain.CurrentDomain.BaseDirectory;
        var path = Path.Combine(root, parentFolder, folder);
        if (!Directory.Exists(path))
        {
            StaticLogger.LogWarning($"\"{dir}\" does not exist! ScriptRunner exited.");
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
                    sqlContext.ExecuteNonQuery(script);
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
}
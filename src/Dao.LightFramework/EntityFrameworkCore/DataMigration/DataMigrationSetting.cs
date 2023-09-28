namespace Dao.LightFramework.EntityFrameworkCore.DataMigration;

public class DataMigrationSetting
{
    public string DBScriptsFolder { get; set; } = "DBScripts";
    public string OnMigratingFolder { get; set; } = "OnMigrating";
    public string OnMigratedFolder { get; set; } = "OnMigrated";

    public Dictionary<string, string> Replacements { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
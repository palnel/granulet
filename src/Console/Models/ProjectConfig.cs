namespace Granulet.Console.Models;

public class ProjectConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public string MigrationsPath { get; set; } = "migrations";
    public string HistoryTableSchema { get; set; } = "dbo";
    public string HistoryTableName { get; set; } = "__MigrationHistory";
}


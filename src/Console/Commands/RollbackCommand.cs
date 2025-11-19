using System;
using System.CommandLine;
using Granulet.Console.Services;

namespace Granulet.Console.Commands;

public class RollbackCommand
{
    public static Command Create()
    {
        var command = new Command("rollback", "Rollback the last applied migration");

        command.SetHandler(async () =>
        {
            await HandleAsync();
        });

        return command;
    }

    private static async Task HandleAsync()
    {
        var configService = new ConfigService();
        var projectDir = configService.FindConfigDirectory();

        if (projectDir == null)
        {
            System.Console.WriteLine("❌ Not in a Granulet project. Run 'gran init' first.");
            return;
        }

        var config = configService.LoadConfig(projectDir);
        if (config == null)
        {
            System.Console.WriteLine("❌ Failed to load configuration.");
            return;
        }

        var migrationsPath = Path.Combine(projectDir, config.MigrationsPath);
        var migrationService = new MigrationService();
        var databaseService = new DatabaseService(
            config.ConnectionString,
            config.HistoryTableSchema,
            config.HistoryTableName
        );

        try
        {
            await databaseService.EnsureHistoryTableExistsAsync();
            var lastMigration = await databaseService.GetLastAppliedMigrationAsync();

            if (lastMigration == null)
            {
                System.Console.WriteLine("❌ No migrations have been applied.");
                return;
            }

            // Find the migration file
            var migrationFile = migrationService.FindMigrationByName(migrationsPath, lastMigration.ScriptName);
            if (migrationFile == null)
            {
                System.Console.WriteLine($"❌ Migration file '{lastMigration.ScriptName}' not found.");
                return;
            }

            // Reload the file content to get the latest version
            migrationFile.Content = File.ReadAllText(migrationFile.FullPath);

            System.Console.WriteLine($"Rolling back: {migrationFile.FileName}");
            System.Console.WriteLine($"Applied at: {lastMigration.AppliedAt:yyyy-MM-dd HH:mm:ss}");
            System.Console.WriteLine();

            await databaseService.RollbackMigrationAsync(migrationFile);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                System.Console.WriteLine($"   {ex.InnerException.Message}");
            }
        }
    }
}


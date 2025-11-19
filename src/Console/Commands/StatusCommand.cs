using System;
using System.CommandLine;
using Granulet.Console.Services;

namespace Granulet.Console.Commands;

public class StatusCommand
{
    public static Command Create()
    {
        var command = new Command("status", "Show applied vs pending migrations");

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
            var appliedMigrations = await databaseService.GetAppliedMigrationsAsync();
            var allMigrations = migrationService.GetMigrationFiles(migrationsPath);

            var appliedVersions = appliedMigrations.Select(m => m.Version).ToHashSet();
            var applied = allMigrations.Where(m => appliedVersions.Contains(m.Version)).ToList();
            var pending = allMigrations.Where(m => !appliedVersions.Contains(m.Version)).ToList();

            System.Console.WriteLine("Applied:");
            if (applied.Any())
            {
                foreach (var migration in applied)
                {
                    var history = appliedMigrations.First(m => m.Version == migration.Version);
                    System.Console.WriteLine($"  {migration.FileName} ({history.AppliedAt:yyyy-MM-dd HH:mm:ss})");
                }
            }
            else
            {
                System.Console.WriteLine("  (none)");
            }

            System.Console.WriteLine();
            System.Console.WriteLine("Pending:");
            if (pending.Any())
            {
                foreach (var migration in pending)
                {
                    System.Console.WriteLine($"  {migration.FileName}");
                }
            }
            else
            {
                System.Console.WriteLine("  (none)");
            }

            System.Console.WriteLine();

            var currentVersion = applied.LastOrDefault()?.FileName ?? "(none)";
            System.Console.WriteLine($"Current version: {currentVersion}");
            System.Console.WriteLine($"Pending count: {pending.Count}");
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


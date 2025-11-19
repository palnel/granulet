using System;
using System.CommandLine;
using Granulet.Console.Services;

namespace Granulet.Console.Commands;

public class UpdateCommand
{
    public static Command Create()
    {
        var migrationArgument = new Argument<string?>(
            name: "migration",
            getDefaultValue: () => null
        )
        {
            Description = "Migration to apply (filename or 'all' for all pending migrations)"
        };

        var command = new Command("update", "Apply migrations")
        {
            migrationArgument
        };

        command.SetHandler(async (string? migration) =>
        {
            await HandleAsync(migration);
        }, migrationArgument);

        return command;
    }

    private static async Task HandleAsync(string? migration)
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
            var pendingMigrations = allMigrations
                .Where(m => !appliedVersions.Contains(m.Version))
                .OrderBy(m => m.Version)
                .ToList();

            if (migration == null || migration == "")
            {
                // Apply next pending migration
                if (!pendingMigrations.Any())
                {
                    System.Console.WriteLine("✅ No pending migrations.");
                    return;
                }

                var next = pendingMigrations.First();
                await ApplyMigrationAsync(databaseService, next);
            }
            else if (migration.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                // Apply all pending migrations
                if (!pendingMigrations.Any())
                {
                    System.Console.WriteLine("✅ No pending migrations.");
                    return;
                }

                System.Console.WriteLine($"Applying {pendingMigrations.Count} migration(s)...");
                System.Console.WriteLine();

                foreach (var mig in pendingMigrations)
                {
                    await ApplyMigrationAsync(databaseService, mig);
                }

                System.Console.WriteLine();
                System.Console.WriteLine($"✅ Applied {pendingMigrations.Count} migration(s) successfully.");
            }
            else
            {
                // Apply specific migration
                var targetMigration = migrationService.FindMigrationByName(migrationsPath, migration);
                if (targetMigration == null)
                {
                    System.Console.WriteLine($"❌ Migration '{migration}' not found.");
                    return;
                }

                if (appliedVersions.Contains(targetMigration.Version))
                {
                    System.Console.WriteLine($"⚠️  Migration '{targetMigration.FileName}' has already been applied.");
                    return;
                }

                await ApplyMigrationAsync(databaseService, targetMigration);
            }
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

    private static async Task ApplyMigrationAsync(DatabaseService databaseService, Models.MigrationFile migration)
    {
        System.Console.WriteLine($"Applying: {migration.FileName}...");
        
        try
        {
            var history = await databaseService.ExecuteMigrationAsync(migration);
            System.Console.WriteLine($"✅ Applied: {migration.FileName} ({history.ExecutionDurationMs}ms)");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ Failed: {migration.FileName}");
            System.Console.WriteLine($"   {ex.Message}");
            throw;
        }
    }
}


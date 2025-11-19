using System;
using System.CommandLine;
using Granulet.Console.Services;

namespace Granulet.Console.Commands;

public class NewCommand
{
    public static Command Create()
    {
        var nameArgument = new Argument<string>(name: "name")
        {
            Description = "Name of the migration (e.g., add_users_table)"
        };

        var command = new Command("new", "Generate a new migration file")
        {
            nameArgument
        };

        command.SetHandler(async (string name) =>
        {
            await HandleAsync(name);
        }, nameArgument);

        return command;
    }

    private static async Task HandleAsync(string name)
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

        var migration = migrationService.CreateMigrationFile(migrationsPath, name);
        if (migration == null)
        {
            System.Console.WriteLine($"❌ Failed to create migration file.");
            return;
        }

        System.Console.WriteLine($"✅ Created migration: {migration.FileName}");
        System.Console.WriteLine($"📄 Path: {migration.FullPath}");
        System.Console.WriteLine();
        System.Console.WriteLine("Edit the file and add your SQL statements.");
    }
}


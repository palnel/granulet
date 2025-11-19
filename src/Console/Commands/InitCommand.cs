using System;
using System.CommandLine;
using Granulet.Console.Services;

namespace Granulet.Console.Commands;

public class InitCommand
{
    public static Command Create()
    {
        var projectNameArgument = new Argument<string>(
            name: "project-name",
            getDefaultValue: () => "."
        )
        {
            Description = "Name of the project to create, or '.' to initialize in current directory"
        };

        var command = new Command("init", "Create a new Granulet project")
        {
            projectNameArgument
        };

        command.SetHandler(async (string projectName) =>
        {
            await HandleAsync(projectName);
        }, projectNameArgument);

        return command;
    }

    private static async Task HandleAsync(string projectName)
    {
        var configService = new ConfigService();
        string targetDirectory;

        if (projectName == ".")
        {
            targetDirectory = Directory.GetCurrentDirectory();
            
            // Check if already initialized
            if (configService.FindConfigDirectory(targetDirectory) != null)
            {
                System.Console.WriteLine("❌ A Granulet project already exists in this directory.");
                return;
            }
        }
        else
        {
            targetDirectory = Path.Combine(Directory.GetCurrentDirectory(), projectName);
            
            if (Directory.Exists(targetDirectory))
            {
                System.Console.WriteLine($"❌ Directory '{projectName}' already exists.");
                return;
            }

            Directory.CreateDirectory(targetDirectory);
        }

        // Create migrations directory
        var migrationsPath = Path.Combine(targetDirectory, "migrations");
        Directory.CreateDirectory(migrationsPath);

        // Create default config
        var config = new Models.ProjectConfig
        {
            ConnectionString = "Server=YOUR_SERVER;Database=YOUR_DB;Trusted_Connection=True;TrustServerCertificate=True;",
            MigrationsPath = "migrations",
            HistoryTableSchema = "dbo",
            HistoryTableName = "__MigrationHistory"
        };

        configService.SaveConfig(config, targetDirectory);

        System.Console.WriteLine($"✅ Granulet project initialized in '{targetDirectory}'");
        System.Console.WriteLine($"📁 Created migrations directory: {migrationsPath}");
        System.Console.WriteLine($"⚙️  Configuration file: granulet.config.json");
        System.Console.WriteLine();
        System.Console.WriteLine("Next steps:");
        System.Console.WriteLine("  1. Edit granulet.config.json and set your connection string");
        System.Console.WriteLine("  2. Create a migration: gran new <name>");
    }
}


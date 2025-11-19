using Granulet.Console.Models;

namespace Granulet.Console.Services;

public class MigrationService
{
    public List<MigrationFile> GetMigrationFiles(string migrationsPath)
    {
        if (!Directory.Exists(migrationsPath))
            return new List<MigrationFile>();

        var files = Directory.GetFiles(migrationsPath, "*.sql")
            .Select(filePath =>
            {
                var fileName = Path.GetFileName(filePath);
                var parts = ParseFileName(fileName);
                return new MigrationFile
                {
                    FileName = fileName,
                    FullPath = filePath,
                    Version = parts.version,
                    Name = parts.name,
                    Content = File.ReadAllText(filePath)
                };
            })
            .OrderBy(m => m.Version)
            .ToList();

        return files;
    }

    public MigrationFile? CreateMigrationFile(string migrationsPath, string name)
    {
        if (!Directory.Exists(migrationsPath))
            Directory.CreateDirectory(migrationsPath);

        var version = GenerateVersionWithSequence(migrationsPath, DateTime.Now);
        var fileName = $"{version}_{name}.sql";
        var filePath = Path.Combine(migrationsPath, fileName);

        if (File.Exists(filePath))
            return null;

        var template = $@"-- =============================================
-- Migration: {name}
-- Version: {version}
-- Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
-- =============================================

-- =============================================
-- UP Migration (Apply)
-- =============================================
-- This section contains the forward migration SQL.
-- It will be executed when you run: gran update
-- 
-- Add your forward migration SQL here:
-- Example:
-- CREATE TABLE dbo.Users (
--     UserId INT IDENTITY(1,1) PRIMARY KEY,
--     Username NVARCHAR(100) NOT NULL,
--     Email NVARCHAR(255) NOT NULL,
--     CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
-- );

GO

-- =============================================
-- DOWN Migration (Rollback)
-- =============================================
-- This section contains the rollback migration SQL.
-- It will be executed when you run: gran rollback
-- 
-- Add your rollback SQL here (should undo the UP migration):
-- Example:
-- DROP TABLE IF EXISTS dbo.Users;

GO
";

        var migration = new MigrationFile
        {
            FileName = fileName,
            FullPath = filePath,
            Version = version,
            Name = name,
            Content = template
        };

        File.WriteAllText(filePath, template);
        return migration;
    }

    public MigrationFile? FindMigrationByName(string migrationsPath, string nameOrFileName)
    {
        var files = GetMigrationFiles(migrationsPath);
        
        // Try exact filename match first
        var exactMatch = files.FirstOrDefault(f => 
            f.FileName.Equals(nameOrFileName, StringComparison.OrdinalIgnoreCase) ||
            f.FileName.Equals($"{nameOrFileName}.sql", StringComparison.OrdinalIgnoreCase));
        
        if (exactMatch != null)
            return exactMatch;

        // Try partial match on name
        return files.FirstOrDefault(f => 
            f.Name.Equals(nameOrFileName, StringComparison.OrdinalIgnoreCase) ||
            f.FileName.Contains(nameOrFileName, StringComparison.OrdinalIgnoreCase));
    }

    private (string version, string name) ParseFileName(string fileName)
    {
        // Format: YYYY.MM.DD_NNN_name.sql
        var withoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var parts = withoutExtension.Split('_');
        
        if (parts.Length >= 3)
        {
            // Version is YYYY.MM.DD_NNN, name is everything after
            var version = $"{parts[0]}_{parts[1]}";
            var name = string.Join("_", parts.Skip(2));
            return (version, name);
        }
        else if (parts.Length == 2)
        {
            // Fallback: assume first part is version, second is name
            return (parts[0], parts[1]);
        }

        return (withoutExtension, withoutExtension);
    }

    public string GenerateVersionWithSequence(string migrationsPath, DateTime date)
    {
        var datePart = date.ToString("yyyy.MM.dd");
        var existingFiles = GetMigrationFiles(migrationsPath);
        
        var todayFiles = existingFiles
            .Where(f => f.Version.StartsWith(datePart + "_"))
            .ToList();

        var maxSequence = 0;
        foreach (var file in todayFiles)
        {
            // Version format: YYYY.MM.DD_NNN
            var parts = file.Version.Split('_');
            if (parts.Length >= 2 && int.TryParse(parts[1], out var seq))
            {
                maxSequence = Math.Max(maxSequence, seq);
            }
        }

        var nextSequence = maxSequence + 1;
        return $"{datePart}_{nextSequence:D3}";
    }
}


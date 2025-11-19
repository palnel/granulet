using System.Text.Json;
using Granulet.Console.Models;

namespace Granulet.Console.Services;

public class ConfigService
{
    private const string ConfigFileName = "granulet.config.json";

    public ProjectConfig? LoadConfig(string? directory = null)
    {
        var configPath = GetConfigPath(directory);
        if (!File.Exists(configPath))
            return null;

        var json = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<ProjectConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public void SaveConfig(ProjectConfig config, string directory)
    {
        var configPath = Path.Combine(directory, ConfigFileName);
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(configPath, json);
    }

    public string? FindConfigDirectory(string? startDirectory = null)
    {
        var current = startDirectory ?? Directory.GetCurrentDirectory();
        var root = new DirectoryInfo(current).Root.FullName;

        while (current != root)
        {
            var configPath = Path.Combine(current, ConfigFileName);
            if (File.Exists(configPath))
                return current;

            var parent = Directory.GetParent(current);
            if (parent == null)
                break;

            current = parent.FullName;
        }

        return null;
    }

    private string GetConfigPath(string? directory)
    {
        if (directory != null)
            return Path.Combine(directory, ConfigFileName);

        var configDir = FindConfigDirectory();
        if (configDir == null)
            return Path.Combine(Directory.GetCurrentDirectory(), ConfigFileName);

        return Path.Combine(configDir, ConfigFileName);
    }
}


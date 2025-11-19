namespace Granulet.Console.Models;

public class MigrationHistory
{
    public string Version { get; set; } = string.Empty;
    public string ScriptName { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }
    public long ExecutionDurationMs { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}


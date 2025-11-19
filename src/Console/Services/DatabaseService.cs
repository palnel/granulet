using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Granulet.Console.Models;

namespace Granulet.Console.Services;

public class DatabaseService
{
    private readonly string _connectionString;
    private readonly string _historyTableSchema;
    private readonly string _historyTableName;

    public DatabaseService(string connectionString, string historyTableSchema, string historyTableName)
    {
        _connectionString = connectionString;
        _historyTableSchema = historyTableSchema;
        _historyTableName = historyTableName;
    }

    public async Task EnsureHistoryTableExistsAsync()
    {
        var createTableSql = $@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{_historyTableSchema}].[{_historyTableName}]') AND type in (N'U'))
BEGIN
    CREATE TABLE [{_historyTableSchema}].[{_historyTableName}] (
        [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
        [Version] NVARCHAR(100) NOT NULL,
        [ScriptName] NVARCHAR(500) NOT NULL,
        [AppliedAt] DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        [ExecutionDurationMs] BIGINT NOT NULL,
        [Success] BIT NOT NULL,
        [ErrorMessage] NVARCHAR(MAX) NULL,
        CONSTRAINT [UK_{_historyTableName}_Version] UNIQUE ([Version])
    );
    CREATE INDEX [IX_{_historyTableName}_AppliedAt] ON [{_historyTableSchema}].[{_historyTableName}] ([AppliedAt]);
END";

        await ExecuteNonQueryAsync(createTableSql);
    }

    public async Task<List<MigrationHistory>> GetAppliedMigrationsAsync()
    {
        var sql = $@"
SELECT [Version], [ScriptName], [AppliedAt], [ExecutionDurationMs], [Success], [ErrorMessage]
FROM [{_historyTableSchema}].[{_historyTableName}]
WHERE [Success] = 1
ORDER BY [AppliedAt]";

        var migrations = new List<MigrationHistory>();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            migrations.Add(new MigrationHistory
            {
                Version = reader.GetString(0),
                ScriptName = reader.GetString(1),
                AppliedAt = reader.GetDateTime(2),
                ExecutionDurationMs = reader.GetInt64(3),
                Success = reader.GetBoolean(4),
                ErrorMessage = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        return migrations;
    }

    public async Task<bool> IsMigrationAppliedAsync(string version)
    {
        var sql = $@"
SELECT COUNT(1)
FROM [{_historyTableSchema}].[{_historyTableName}]
WHERE [Version] = @Version AND [Success] = 1";

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Version", version);

        var result = await command.ExecuteScalarAsync();
        var count = result != null ? (int)result : 0;
        return count > 0;
    }

    public async Task<MigrationHistory> ExecuteMigrationAsync(MigrationFile migration)
    {
        var stopwatch = Stopwatch.StartNew();
        var history = new MigrationHistory
        {
            Version = migration.Version,
            ScriptName = migration.FileName,
            AppliedAt = DateTime.UtcNow,
            Success = false
        };

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        try
        {
            // Extract and execute the UP section
            var sql = ExtractUpSection(migration.Content);
            
            // Split by GO statements (case-insensitive)
            var batches = SplitSqlBatches(sql);
            
            foreach (var batch in batches)
            {
                if (string.IsNullOrWhiteSpace(batch))
                    continue;

                using var command = new SqlCommand(batch, connection, transaction);
                await command.ExecuteNonQueryAsync();
            }

            transaction.Commit();
            stopwatch.Stop();

            history.Success = true;
            history.ExecutionDurationMs = stopwatch.ElapsedMilliseconds;

            // Record in history
            await RecordMigrationHistoryAsync(connection, history);

            return history;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            stopwatch.Stop();

            history.Success = false;
            history.ExecutionDurationMs = stopwatch.ElapsedMilliseconds;
            history.ErrorMessage = ex.Message;

            // Record failure in history
            await RecordMigrationHistoryAsync(connection, history);

            throw new Exception($"Migration failed: {ex.Message}", ex);
        }
    }

    public async Task RollbackMigrationAsync(MigrationFile migrationFile)
    {
        var stopwatch = Stopwatch.StartNew();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        try
        {
            // Extract and execute the DOWN section
            var sql = ExtractDownSection(migrationFile.Content);
            
            if (string.IsNullOrWhiteSpace(sql.Trim()))
            {
                throw new Exception("No DOWN migration section found. Cannot rollback.");
            }

            // Split by GO statements (case-insensitive)
            var batches = SplitSqlBatches(sql);
            
            foreach (var batch in batches)
            {
                if (string.IsNullOrWhiteSpace(batch))
                    continue;

                using var command = new SqlCommand(batch, connection, transaction);
                await command.ExecuteNonQueryAsync();
            }

            // Remove from history
            var deleteSql = $@"
DELETE FROM [{_historyTableSchema}].[{_historyTableName}]
WHERE [Version] = @Version AND [Success] = 1";

            using var deleteCommand = new SqlCommand(deleteSql, connection, transaction);
            deleteCommand.Parameters.AddWithValue("@Version", migrationFile.Version);
            await deleteCommand.ExecuteNonQueryAsync();

            transaction.Commit();
            stopwatch.Stop();

            System.Console.WriteLine($"✅ Rolled back: {migrationFile.FileName} ({stopwatch.ElapsedMilliseconds}ms)");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            stopwatch.Stop();
            throw new Exception($"Rollback failed: {ex.Message}", ex);
        }
    }

    public async Task<MigrationHistory?> GetLastAppliedMigrationAsync()
    {
        var sql = $@"
SELECT TOP 1 [Version], [ScriptName], [AppliedAt], [ExecutionDurationMs], [Success], [ErrorMessage]
FROM [{_historyTableSchema}].[{_historyTableName}]
WHERE [Success] = 1
ORDER BY [AppliedAt] DESC";

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new MigrationHistory
            {
                Version = reader.GetString(0),
                ScriptName = reader.GetString(1),
                AppliedAt = reader.GetDateTime(2),
                ExecutionDurationMs = reader.GetInt64(3),
                Success = reader.GetBoolean(4),
                ErrorMessage = reader.IsDBNull(5) ? null : reader.GetString(5)
            };
        }

        return null;
    }

    private async Task RecordMigrationHistoryAsync(SqlConnection connection, MigrationHistory history)
    {
        var sql = $@"
INSERT INTO [{_historyTableSchema}].[{_historyTableName}]
([Version], [ScriptName], [AppliedAt], [ExecutionDurationMs], [Success], [ErrorMessage])
VALUES
(@Version, @ScriptName, @AppliedAt, @ExecutionDurationMs, @Success, @ErrorMessage)";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Version", history.Version);
        command.Parameters.AddWithValue("@ScriptName", history.ScriptName);
        command.Parameters.AddWithValue("@AppliedAt", history.AppliedAt);
        command.Parameters.AddWithValue("@ExecutionDurationMs", history.ExecutionDurationMs);
        command.Parameters.AddWithValue("@Success", history.Success);
        command.Parameters.AddWithValue("@ErrorMessage", (object?)history.ErrorMessage ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    private async Task ExecuteNonQueryAsync(string sql)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private List<string> SplitSqlBatches(string sql)
    {
        // Simple batch splitting by GO statements
        // This is a basic implementation - can be enhanced for more complex scenarios
        var batches = new List<string>();
        var lines = sql.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var currentBatch = new System.Text.StringBuilder();

        foreach (var line in lines)
        {
            if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                if (currentBatch.Length > 0)
                {
                    batches.Add(currentBatch.ToString());
                    currentBatch.Clear();
                }
            }
            else
            {
                currentBatch.AppendLine(line);
            }
        }

        if (currentBatch.Length > 0)
        {
            batches.Add(currentBatch.ToString());
        }

        return batches;
    }

    private string ExtractUpSection(string content)
    {
        // Look for UP Migration section
        var upMarker = "-- UP Migration";
        var downMarker = "-- DOWN Migration";
        
        var upIndex = content.IndexOf(upMarker, StringComparison.OrdinalIgnoreCase);
        var downIndex = content.IndexOf(downMarker, StringComparison.OrdinalIgnoreCase);

        if (upIndex == -1)
        {
            // No UP section marker found, return entire content (backward compatibility)
            if (downIndex == -1)
                return content;
            
            // If only DOWN section exists, return everything before it
            return content.Substring(0, downIndex).Trim();
        }

        // Extract content between UP and DOWN markers
        var endIndex = downIndex == -1 ? content.Length : downIndex;
        var sectionContent = content.Substring(upIndex, endIndex - upIndex);
        
        // Split into lines and extract SQL (skip comments and separators)
        var lines = sectionContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var sqlLines = new List<string>();
        bool inSqlSection = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Skip marker and separator lines
            if (trimmed.Contains("-- UP Migration", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("==="))
            {
                inSqlSection = true;
                continue;
            }
            
            // Once we're past the marker, collect all lines (including comments within SQL)
            if (inSqlSection)
            {
                sqlLines.Add(line);
            }
        }

        return string.Join("\n", sqlLines).Trim();
    }

    private string ExtractDownSection(string content)
    {
        // Look for DOWN Migration section
        var downMarker = "-- DOWN Migration";
        
        var downIndex = content.IndexOf(downMarker, StringComparison.OrdinalIgnoreCase);

        if (downIndex == -1)
        {
            // No DOWN section marker found, return empty
            return string.Empty;
        }

        // Extract content after DOWN marker
        var sectionContent = content.Substring(downIndex);
        
        // Split into lines and extract SQL (skip comments and separators)
        var lines = sectionContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var sqlLines = new List<string>();
        bool inSqlSection = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Skip marker and separator lines
            if (trimmed.Contains("-- DOWN Migration", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("==="))
            {
                inSqlSection = true;
                continue;
            }
            
            // Once we're past the marker, collect all lines (including comments within SQL)
            if (inSqlSection)
            {
                sqlLines.Add(line);
            }
        }

        return string.Join("\n", sqlLines).Trim();
    }
}


# Granulet v1.0.0 - Release Notes

**Release Date:** November 19, 2025

## 🎉 Initial Release

Granulet is a lightweight, script-first SQL Server database migration CLI tool designed for developers, DBAs, and DevOps teams who want full control over their database evolution.

## ✨ Features

### Core Functionality

- **Project Management**
  - Initialize new migration projects with `gran init`
  - Project-based configuration via `granulet.config.json`
  - Automatic creation of migrations directory structure

- **Migration Generation**
  - Create versioned migration files with `gran new <name>`
  - Automatic versioning format: `YYYY.MM.DD_NNN_name.sql`
  - Pre-populated templates with UP and DOWN sections
  - Clear comments and examples in each migration file

- **Migration Tracking**
  - View applied vs pending migrations with `gran status`
  - Automatic `__MigrationHistory` table management
  - Tracks execution duration, timestamps, and success/failure status

- **Migration Execution**
  - Apply next pending migration: `gran update`
  - Apply all pending migrations: `gran update all`
  - Apply specific migration: `gran update <migration-name>`
  - Transactional execution with automatic rollback on failure
  - Ordered execution by version (filename prefix)

- **Rollback Support**
  - Rollback last applied migration: `gran rollback`
  - Uses DOWN section from migration files
  - Automatic history cleanup after successful rollback

### Safety Features

- ✅ Each migration runs inside a SQL transaction
- ✅ Strict ordering by version (filename prefix)
- ✅ Stops immediately on any failure
- ✅ Logs success/failure into history table
- ✅ Prevents accidental re-execution of applied migrations

## 📦 Installation

### As a Global Tool

```bash
dotnet tool install -g Granulet.Cli
```

Then use from anywhere:
```bash
granulet --help
```

### As a Local Tool

```bash
dotnet tool install --add-source ./nupkg Granulet.Cli
```

Then use with:
```bash
dotnet granulet --help
```

## 🚀 Quick Start

### 1. Initialize a Project

```bash
gran init MyDatabaseProject
cd MyDatabaseProject
```

### 2. Configure Connection

Edit `granulet.config.json` and set your SQL Server connection string:

```json
{
  "ConnectionString": "Server=YOUR_SERVER;Database=YOUR_DB;Trusted_Connection=True;TrustServerCertificate=True;",
  "MigrationsPath": "migrations",
  "HistoryTableSchema": "dbo",
  "HistoryTableName": "__MigrationHistory"
}
```

### 3. Create a Migration

```bash
gran new add_users_table
```

This generates a file like `2025.11.19_001_add_users_table.sql` with UP and DOWN sections:

```sql
-- =============================================
-- UP Migration (Apply)
-- =============================================
-- This section contains the forward migration SQL.
-- It will be executed when you run: gran update
-- 
-- Add your forward migration SQL here:
CREATE TABLE dbo.Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);

GO

-- =============================================
-- DOWN Migration (Rollback)
-- =============================================
-- This section contains the rollback migration SQL.
-- It will be executed when you run: gran rollback
-- 
-- Add your rollback SQL here (should undo the UP migration):
DROP TABLE IF EXISTS dbo.Users;

GO
```

### 4. Check Status

```bash
gran status
```

### 5. Apply Migrations

```bash
# Apply next pending migration
gran update

# Apply all pending migrations
gran update all

# Apply specific migration
gran update 2025.11.19_001_add_users_table
```

### 6. Rollback

```bash
gran rollback
```

## 📋 Command Reference

| Command | Description |
|---------|-------------|
| `gran init <project-name \| .>` | Create a new Granulet project |
| `gran new <name>` | Generate a new migration file |
| `gran status` | Show applied vs pending migrations |
| `gran update` | Run the next pending migration |
| `gran update all` | Run all pending migrations |
| `gran update <migration>` | Run a specific migration |
| `gran rollback` | Rollback the last applied migration |

## 🗄️ Migration History

Granulet automatically creates and manages the `__MigrationHistory` table in your database:

```sql
[dbo].[__MigrationHistory]
```

This table records:
- Version (migration identifier)
- Script name
- Applied timestamp
- Execution duration (milliseconds)
- Success flag
- Error message (if any)

## 🔧 Configuration

The `granulet.config.json` file supports:

- **ConnectionString**: SQL Server connection string
- **MigrationsPath**: Path to migrations folder (default: "migrations")
- **HistoryTableSchema**: Schema for history table (default: "dbo")
- **HistoryTableName**: Name of history table (default: "__MigrationHistory")

## 📁 Project Structure

```
MyDatabaseProject/
│
├── granulet.config.json     # project configuration
└── migrations/              # migration files
      2025.11.19_001_init.sql
      2025.11.19_002_add_users_table.sql
      2025.11.19_003_add_products_table.sql
```

## 🛠️ Technical Details

- **Framework**: .NET 10.0
- **Dependencies**:
  - System.CommandLine (2.0.0-beta4.22272.1)
  - Microsoft.Data.SqlClient (5.2.2)
- **Platform**: Cross-platform (Windows, Linux, macOS)

## 🔐 Safety Mechanisms

- **Transactional Execution**: All migrations run in transactions
- **Ordered Execution**: Migrations are applied in version order
- **Failure Handling**: Automatic rollback on any error
- **History Tracking**: Complete audit trail of all migrations
- **Idempotency**: Prevents re-execution of already applied migrations

## 📝 Migration File Format

Migration files follow the naming convention:
```
YYYY.MM.DD_NNN_name.sql
```

Where:
- `YYYY.MM.DD` is the date
- `NNN` is a 3-digit sequence number (001, 002, etc.)
- `name` is the migration name

Each file contains:
- **UP section**: Forward migration SQL (executed on `gran update`)
- **DOWN section**: Rollback SQL (executed on `gran rollback`)

## 🐛 Known Limitations

- Rollback requires DOWN section to be present in the migration file
- Migration files must follow the versioned naming convention
- Connection string must be valid SQL Server connection string

## 🗺️ Roadmap

Future enhancements planned:
- Rollback templates
- Environment support (`--env dev | test | prod`)
- Verbose execution logs
- Hash consistency check
- Hooks (before/after events)
- CI/CD pipeline examples
- Packaging as .exe installer

**Thank you for using Granulet!** 🚀


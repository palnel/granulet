# Changelog

All notable changes to Granulet will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-11-19

### Added

#### Core Features
- **Project Initialization**: `gran init` command to create new Granulet projects
  - Creates project directory structure
  - Generates default `granulet.config.json` configuration file
  - Creates empty `migrations/` folder
  - Supports initializing in current directory with `gran init .`

- **Migration Generation**: `gran new <name>` command
  - Automatic versioning with format `YYYY.MM.DD_NNN_name.sql`
  - Sequential numbering for same-day migrations
  - Pre-populated template with UP and DOWN sections
  - Clear comments and examples in migration files
  - Header with migration metadata (name, version, generation timestamp)

- **Migration Status**: `gran status` command
  - Lists all applied migrations with timestamps
  - Lists all pending migrations
  - Shows current version
  - Displays pending migration count
  - Connects to database to query migration history

- **Migration Execution**: `gran update` command
  - Apply next pending migration: `gran update`
  - Apply all pending migrations: `gran update all`
  - Apply specific migration: `gran update <migration-name>`
  - Supports partial filename matching
  - Transactional execution with automatic rollback on failure
  - Ordered execution by version (filename prefix)
  - Execution duration tracking
  - Success/failure logging

- **Migration Rollback**: `gran rollback` command
  - Rollback last applied migration
  - Extracts and executes DOWN section from migration file
  - Removes migration from history after successful rollback
  - Transactional rollback with error handling
  - Execution duration tracking

#### Database Management
- **Automatic History Table Creation**: `__MigrationHistory` table
  - Creates table on first migration operation
  - Configurable schema and table name via config
  - Tracks version, script name, applied timestamp, duration, success, and error messages
  - Unique constraint on version to prevent duplicates
  - Indexed on applied timestamp for performance

- **Migration History Tracking**
  - Records all migration attempts (success and failure)
  - Tracks execution duration in milliseconds
  - Stores error messages for failed migrations
  - Prevents re-execution of already applied migrations

#### Configuration
- **Project-based Configuration**: `granulet.config.json`
  - Connection string configuration
  - Migrations path configuration
  - History table schema and name configuration
  - JSON-based configuration file
  - Automatic config file discovery (searches parent directories)

#### Migration File Format
- **UP/DOWN Migration Sections**
  - UP section for forward migrations (applied on `gran update`)
  - DOWN section for rollback migrations (executed on `gran rollback`)
  - Clear section markers with comments
  - Example SQL code in templates
  - Support for GO batch separators

- **Versioned File Naming**
  - Format: `YYYY.MM.DD_NNN_name.sql`
  - Automatic sequence numbering
  - Date-based versioning
  - Human-readable migration names

#### Safety Features
- **Transactional Execution**
  - All migrations run inside SQL transactions
  - Automatic rollback on any error
  - Prevents partial migrations

- **Ordered Execution**
  - Strict ordering by version (filename prefix)
  - Prevents out-of-order execution
  - Validates migration sequence

- **Error Handling**
  - Stops immediately on any failure
  - Logs errors to history table
  - Clear error messages to user
  - Transaction rollback on exceptions

- **Idempotency**
  - Prevents accidental re-execution
  - Checks history before applying
  - Unique version constraint in database

#### CLI Features
- **Command-line Interface**
  - Built with System.CommandLine
  - Help text for all commands
  - Version information
  - Clear error messages
  - User-friendly output with emojis

- **Global Tool Support**
  - Packaged as .NET global tool
  - Installable via `dotnet tool install`
  - Command name: `granulet` (short alias: `gran`)
  - Cross-platform support

### Technical Details

#### Dependencies
- .NET 10.0
- System.CommandLine (2.0.0-beta4.22272.1)
- Microsoft.Data.SqlClient (5.2.2)

#### Architecture
- Modular service architecture
  - `ConfigService`: Configuration management
  - `MigrationService`: Migration file operations
  - `DatabaseService`: SQL Server operations
- Command-based CLI structure
  - `InitCommand`: Project initialization
  - `NewCommand`: Migration generation
  - `StatusCommand`: Status display
  - `UpdateCommand`: Migration execution
  - `RollbackCommand`: Migration rollback

#### File Structure
```
src/Console/
├── Models/
│   ├── ProjectConfig.cs
│   ├── MigrationHistory.cs
│   └── MigrationFile.cs
├── Services/
│   ├── ConfigService.cs
│   ├── MigrationService.cs
│   └── DatabaseService.cs
├── Commands/
│   ├── InitCommand.cs
│   ├── NewCommand.cs
│   ├── StatusCommand.cs
│   ├── UpdateCommand.cs
│   └── RollbackCommand.cs
└── Program.cs
```

### Documentation
- Comprehensive README.md with usage examples
- Release notes (RELEASE_NOTES.md)
- This changelog (CHANGELOG.md)
- Inline code comments
- Command help text

### Known Limitations
- Rollback requires DOWN section to be present in migration file
- Migration files must follow versioned naming convention
- Connection string must be valid SQL Server connection string
- No support for multiple environments yet
- No hash consistency checking yet
- No hooks/events system yet

---

## [Unreleased]

### Planned Features
- Rollback templates
- Environment support (`--env dev | test | prod`)
- Verbose execution logs
- Hash consistency check
- Hooks (before/after events)
- CI/CD pipeline examples
- Packaging as .exe installer
- Migration validation
- Dry-run mode
- Migration dependencies
- Custom migration templates

---

[1.0.0]: https://github.com/palnel/granulet/releases/tag/v1.0.0


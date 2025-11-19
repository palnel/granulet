# **Granulet — SQL Server Database Migration CLI**

**Granulet** is a lightweight, script-first, SQL Server database migration tool designed for developers, DBAs, and DevOps teams who want full control over their database evolution.
It runs from the terminal using the command:

```
granulet
```

Or the short alias:

```
gran
```

Granulet helps you:

* Create structured migration projects
* Generate new migrations
* Track applied vs pending migrations
* Apply migrations safely and predictably
* Roll back changes
* Automate deployments in CI/CD

It’s simple. It’s transparent. It’s built for teams who need reliability without heavy tooling.

---

## **🚀 Features**

* Full CLI tool (install once → use anywhere)
* Create migration projects instantly
* Generate versioned migration files
* Track database state with `status`
* Apply migrations incrementally
* Apply all pending migrations at once
* Execute a specific migration on demand
* Roll back the last executed migration
* Project-based config file for multiple DBs
* Automatic creation & management of `__MigrationHistory`

---

## **📦 Installation**

> Final installation method will depend on your packaging (NuGet global tool, EXE installer, standalone binary).
> Placeholder example:

```
dotnet tool install -g Granulet.Cli
```

Then use:

```
gran
```

---

## **📁 Project Structure**

When you initialize a new project:

```
MyDatabaseProject/
│
├── granulet.config.json     # project configuration
└── migrations/              # migration files
      2025.01.01_001_init.sql
      2025.01.01_010_create_schema.sql
      2025.01.02_001_add_users_table.sql
```

### Example: `granulet.config.json`

```json
{
  "ConnectionString": "Server=YOUR_SERVER;Database=YOUR_DB;Trusted_Connection=True;TrustServerCertificate=True;",
  "MigrationsPath": "migrations",
  "HistoryTableSchema": "dbo",
  "HistoryTableName": "__MigrationHistory"
}
```

---

## **🏁 Getting Started**

### **1. Create a new project**

```
gran init MyDatabaseProject
cd MyDatabaseProject
```

This will:

* Create the project directory
* Add a default configuration file
* Create an empty `migrations/` folder

---

### **2. Generate a new migration**

```
gran new add_users_table
```

This generates a file like:

```
migrations/
  2025.11.19_001_add_users_table.sql
```

You edit it and write your SQL:

```sql
CREATE TABLE app.Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    Email NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
```

---

### **3. Check migration status**

```
gran status
```

Example output:

```
Applied:
  2025.11.18_001_init.sql
  2025.11.18_002_create_schema.sql

Pending:
  2025.11.19_001_add_users_table.sql

Current version: 2025.11.18_002_create_schema.sql
Pending count: 1
```

---

### **4. Apply migrations (update)**

#### Apply **next pending migration**

```
gran update
```

#### Apply **all pending migrations**

```
gran update all
```

#### Apply a **specific migration**

```
gran update 2025.11.19_001_add_users_table
```

or

```
gran update 2025.11.19_001_add_users_table.sql
```

---

### **5. Roll back the last migration**

```
gran rollback
```

Rollback is based on your project’s convention:

* `*_down.sql` files
  **or**
* rollback blocks embedded inside each migration file

(Exact rollback rules will depend on your implementation.)

---

## **🧾 Command Reference**

### `gran init <project-name | .>`

Create a new Granulet project.

---

### `gran new <name>`

Generate a new migration file.

---

### `gran status`

Show applied vs pending migrations, including timestamps.

---

### `gran update`

Run the **next** pending migration.

### `gran update all`

Run **all** pending migrations.

### `gran update <migration>`

Run a specific migration.

---

### `gran rollback`

Rollback the last applied migration.

---

## **🧠 Migration History**

Granulet creates & manages the table:

```sql
[dbo].[__MigrationHistory]
```

This records:

* Version
* Script name
* Applied timestamp
* Execution duration
* Success flag
* Error message (if any)

No migration runs twice unless explicitly rolled back.

---

## **🔐 Safety Mechanisms**

* Each migration runs inside a SQL transaction
* Ordered strictly by version (filename prefix)
* Stops immediately on any failure
* Logs success/failure into history
* Prevents accidental re-execution

---

## **🛣 Roadmap**

Planned additions:

* Rollback templates
* Environments (`--env dev | test | prod`)
* Verbose execution logs
* Hash consistency check
* Hooks (before/after events)
* CI/CD pipeline examples
* Packaging as .exe installer
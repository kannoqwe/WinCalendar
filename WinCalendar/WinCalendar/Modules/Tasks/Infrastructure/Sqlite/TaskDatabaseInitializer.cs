using System;
using System.IO;

namespace Planner.App.Modules.Tasks.Infrastructure.Sqlite;

public sealed class TaskDatabaseInitializer
{
    private const int CurrentSchemaVersion = 1;
    private readonly string _databasePath;

    public TaskDatabaseInitializer(string databasePath)
    {
        _databasePath = databasePath;
    }

    public void Initialize()
    {
        string? directoryPath = Path.GetDirectoryName(_databasePath);

        if (!string.IsNullOrWhiteSpace(directoryPath))
            Directory.CreateDirectory(directoryPath);

        using SqliteConnection connection = new(_databasePath);
        connection.ExecuteToCompletion("PRAGMA journal_mode = WAL;");

        connection.ExecuteNonQuery(
            """
            CREATE TABLE IF NOT EXISTS tasks (
                id TEXT NOT NULL PRIMARY KEY,
                title TEXT NOT NULL,
                description TEXT NULL,
                task_date TEXT NOT NULL,
                task_time TEXT NULL,
                duration_minutes INTEGER NULL,
                recurrence_pattern TEXT NOT NULL DEFAULT 'None',
                category TEXT NOT NULL DEFAULT 'Work',
                is_completed INTEGER NOT NULL,
                created_at_utc TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL
            );
            """);

        EnsureColumn(
            connection,
            "duration_minutes",
            "ALTER TABLE tasks ADD COLUMN duration_minutes INTEGER NULL;");

        EnsureColumn(
            connection,
            "description",
            "ALTER TABLE tasks ADD COLUMN description TEXT NULL;");

        EnsureColumn(
            connection,
            "recurrence_pattern",
            "ALTER TABLE tasks ADD COLUMN recurrence_pattern TEXT NOT NULL DEFAULT 'None';");

        EnsureColumn(
            connection,
            "category",
            "ALTER TABLE tasks ADD COLUMN category TEXT NOT NULL DEFAULT 'Work';");

        connection.ExecuteNonQuery(
            """
            CREATE INDEX IF NOT EXISTS idx_tasks_task_date
            ON tasks(task_date);
            """);

        connection.ExecuteNonQuery(
            """
            CREATE INDEX IF NOT EXISTS idx_tasks_task_date_task_time_created_at
            ON tasks(task_date, task_time, created_at_utc);
            """);

        connection.ExecuteToCompletion($"PRAGMA user_version = {CurrentSchemaVersion};");
    }

    private static void EnsureColumn(SqliteConnection connection, string columnName, string alterSql)
    {
        using SqliteStatement statement = connection.Prepare("PRAGMA table_info(tasks);");

        while (statement.Read())
        {
            string? existingColumnName = statement.GetText(1);

            if (string.Equals(existingColumnName, columnName, StringComparison.OrdinalIgnoreCase))
                return;
        }

        connection.ExecuteNonQuery(alterSql);
    }
}

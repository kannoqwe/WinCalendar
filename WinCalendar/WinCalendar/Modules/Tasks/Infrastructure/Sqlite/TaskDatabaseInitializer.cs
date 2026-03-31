using System.IO;

namespace Planner.App.Modules.Tasks.Infrastructure.Sqlite;

public sealed class TaskDatabaseInitializer
{
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

        connection.ExecuteNonQuery(
            """
            CREATE TABLE IF NOT EXISTS tasks (
                id TEXT NOT NULL PRIMARY KEY,
                title TEXT NOT NULL,
                task_date TEXT NOT NULL,
                task_time TEXT NULL,
                is_completed INTEGER NOT NULL,
                created_at_utc TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL
            );
            """);

        connection.ExecuteNonQuery(
            """
            CREATE INDEX IF NOT EXISTS idx_tasks_task_date
            ON tasks(task_date);
            """);
    }
}

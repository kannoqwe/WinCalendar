using Planner.App.Modules.Tasks.Contracts;
using Planner.App.Modules.Tasks.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Planner.App.Modules.Tasks.Infrastructure.Sqlite;

public sealed class SqliteTaskRepository : ITaskRepository
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string TimeFormat = "HH:mm:ss";
    private readonly string _databasePath;

    public SqliteTaskRepository(string databasePath)
    {
        _databasePath = databasePath;
    }

    public Task<IReadOnlyList<TaskItem>> GetByDateAsync(DateOnly date)
    {
        List<TaskItem> tasks = [];

        using SqliteConnection connection = OpenConnection();
        using SqliteStatement statement = connection.Prepare(
            """
            SELECT
                id,
                title,
                task_date,
                task_time,
                is_completed,
                created_at_utc,
                updated_at_utc
            FROM tasks
            WHERE task_date = ?
            ORDER BY
                CASE WHEN task_time IS NULL THEN 1 ELSE 0 END,
                task_time,
                created_at_utc;
            """);

        statement.BindText(1, FormatDate(date));

        while (statement.Read())
            tasks.Add(Map(statement));

        return Task.FromResult<IReadOnlyList<TaskItem>>(tasks);
    }

    public Task<TaskItem?> GetByIdAsync(Guid id)
    {
        using SqliteConnection connection = OpenConnection();
        using SqliteStatement statement = connection.Prepare(
            """
            SELECT
                id,
                title,
                task_date,
                task_time,
                is_completed,
                created_at_utc,
                updated_at_utc
            FROM tasks
            WHERE id = ?
            LIMIT 1;
            """);

        statement.BindText(1, id.ToString());

        TaskItem? task = statement.Read() ? Map(statement) : null;
        return Task.FromResult(task);
    }

    public Task AddAsync(TaskItem task)
    {
        using SqliteConnection connection = OpenConnection();
        using SqliteStatement statement = connection.Prepare(
            """
            INSERT INTO tasks (
                id,
                title,
                task_date,
                task_time,
                is_completed,
                created_at_utc,
                updated_at_utc
            )
            VALUES (?, ?, ?, ?, ?, ?, ?);
            """);

        BindTask(statement, task);
        statement.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task UpdateAsync(TaskItem task)
    {
        using SqliteConnection connection = OpenConnection();
        using SqliteStatement statement = connection.Prepare(
            """
            UPDATE tasks
            SET
                title = ?,
                task_date = ?,
                task_time = ?,
                is_completed = ?,
                created_at_utc = ?,
                updated_at_utc = ?
            WHERE id = ?;
            """);

        statement.BindText(1, task.Title);
        statement.BindText(2, FormatDate(task.Date));
        statement.BindNullableText(3, FormatTime(task.Time));
        statement.BindBoolean(4, task.IsCompleted);
        statement.BindText(5, FormatTimestamp(task.CreatedAt));
        statement.BindText(6, FormatTimestamp(task.UpdatedAt));
        statement.BindText(7, task.Id.ToString());
        statement.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        using SqliteConnection connection = OpenConnection();
        using SqliteStatement statement = connection.Prepare("DELETE FROM tasks WHERE id = ?;");

        statement.BindText(1, id.ToString());
        statement.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    private SqliteConnection OpenConnection()
    {
        return new SqliteConnection(_databasePath);
    }

    private static void BindTask(SqliteStatement statement, TaskItem task)
    {
        statement.BindText(1, task.Id.ToString());
        statement.BindText(2, task.Title);
        statement.BindText(3, FormatDate(task.Date));
        statement.BindNullableText(4, FormatTime(task.Time));
        statement.BindBoolean(5, task.IsCompleted);
        statement.BindText(6, FormatTimestamp(task.CreatedAt));
        statement.BindText(7, FormatTimestamp(task.UpdatedAt));
    }

    private static TaskItem Map(SqliteStatement statement)
    {
        Guid id = Guid.Parse(statement.GetText(0)!);
        string title = statement.GetText(1)!;
        DateOnly date = ParseDate(statement.GetText(2)!);
        TimeOnly? time = ParseTime(statement.GetText(3));
        bool isCompleted = statement.GetInt(4) == 1;
        DateTime createdAt = ParseTimestamp(statement.GetText(5)!);
        DateTime updatedAt = ParseTimestamp(statement.GetText(6)!);

        return TaskItem.Restore(id, title, date, time, isCompleted, createdAt, updatedAt);
    }

    private static string FormatDate(DateOnly value)
    {
        return value.ToString(DateFormat, CultureInfo.InvariantCulture);
    }

    private static DateOnly ParseDate(string value)
    {
        return DateOnly.ParseExact(value, DateFormat, CultureInfo.InvariantCulture);
    }

    private static string? FormatTime(TimeOnly? value)
    {
        return value?.ToString(TimeFormat, CultureInfo.InvariantCulture);
    }

    private static TimeOnly? ParseTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return TimeOnly.ParseExact(value, TimeFormat, CultureInfo.InvariantCulture);
    }

    private static string FormatTimestamp(DateTime value)
    {
        return value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    private static DateTime ParseTimestamp(string value)
    {
        return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }
}

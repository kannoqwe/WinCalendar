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
                description,
                task_date,
                task_time,
                duration_minutes,
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

    public Task<IReadOnlyList<TaskItem>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate)
    {
        List<TaskItem> tasks = [];

        using SqliteConnection connection = OpenConnection();
        using SqliteStatement statement = connection.Prepare(
            """
            SELECT
                id,
                title,
                description,
                task_date,
                task_time,
                duration_minutes,
                is_completed,
                created_at_utc,
                updated_at_utc
            FROM tasks
            WHERE task_date BETWEEN ? AND ?
            ORDER BY
                task_date,
                CASE WHEN task_time IS NULL THEN 1 ELSE 0 END,
                task_time,
                created_at_utc;
            """);

        statement.BindText(1, FormatDate(startDate));
        statement.BindText(2, FormatDate(endDate));

        while (statement.Read())
            tasks.Add(Map(statement));

        return Task.FromResult<IReadOnlyList<TaskItem>>(tasks);
    }

    public Task<IReadOnlyList<TaskItem>> GetIncompleteTimedTasksDueBeforeAsync(DateOnly date, TimeOnly time)
    {
        List<TaskItem> tasks = [];

        using SqliteConnection connection = OpenConnection();
        using SqliteStatement statement = connection.Prepare(
            """
            SELECT
                id,
                title,
                description,
                task_date,
                task_time,
                duration_minutes,
                is_completed,
                created_at_utc,
                updated_at_utc
            FROM tasks
            WHERE
                is_completed = 0
                AND task_time IS NOT NULL
                AND duration_minutes IS NOT NULL
                AND (
                    task_date < ?
                    OR (
                        task_date = ?
                        AND (
                            (CAST(strftime('%H', task_time) AS INTEGER) * 60)
                            + CAST(strftime('%M', task_time) AS INTEGER)
                            + duration_minutes
                        ) <= ?
                    )
                )
            ORDER BY
                task_date,
                task_time,
                created_at_utc;
            """);

        string targetDate = FormatDate(date);
        statement.BindText(1, targetDate);
        statement.BindText(2, targetDate);
        statement.BindInt(3, GetMinutesSinceStartOfDay(time));

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
                description,
                task_date,
                task_time,
                duration_minutes,
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
                description,
                task_date,
                task_time,
                duration_minutes,
                is_completed,
                created_at_utc,
                updated_at_utc
            )
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?);
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
                description = ?,
                task_date = ?,
                task_time = ?,
                duration_minutes = ?,
                is_completed = ?,
                created_at_utc = ?,
                updated_at_utc = ?
            WHERE id = ?;
            """);

        statement.BindText(1, task.Title);
        statement.BindNullableText(2, NormalizeNullableText(task.Description));
        statement.BindText(3, FormatDate(task.Date));
        statement.BindNullableText(4, FormatTime(task.Time));
        statement.BindNullableInt(5, task.DurationMinutes);
        statement.BindBoolean(6, task.IsCompleted);
        statement.BindText(7, FormatTimestamp(task.CreatedAt));
        statement.BindText(8, FormatTimestamp(task.UpdatedAt));
        statement.BindText(9, task.Id.ToString());
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
        statement.BindNullableText(3, NormalizeNullableText(task.Description));
        statement.BindText(4, FormatDate(task.Date));
        statement.BindNullableText(5, FormatTime(task.Time));
        statement.BindNullableInt(6, task.DurationMinutes);
        statement.BindBoolean(7, task.IsCompleted);
        statement.BindText(8, FormatTimestamp(task.CreatedAt));
        statement.BindText(9, FormatTimestamp(task.UpdatedAt));
    }

    private static TaskItem Map(SqliteStatement statement)
    {
        Guid id = Guid.Parse(statement.GetText(0)!);
        string title = statement.GetText(1)!;
        string description = statement.GetText(2) ?? string.Empty;
        DateOnly date = ParseDate(statement.GetText(3)!);
        TimeOnly? time = ParseTime(statement.GetText(4));
        int? durationMinutes = statement.GetNullableInt(5);
        bool isCompleted = statement.GetInt(6) == 1;
        DateTime createdAt = ParseTimestamp(statement.GetText(7)!);
        DateTime updatedAt = ParseTimestamp(statement.GetText(8)!);

        return TaskItem.Restore(id, title, date, time, durationMinutes, description, isCompleted, createdAt, updatedAt);
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

    private static string? NormalizeNullableText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value;
    }

    private static TimeOnly? ParseTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return TimeOnly.ParseExact(value, TimeFormat, CultureInfo.InvariantCulture);
    }

    private static int GetMinutesSinceStartOfDay(TimeOnly value)
    {
        return (int)value.ToTimeSpan().TotalMinutes;
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

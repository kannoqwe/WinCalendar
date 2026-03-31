using System;

namespace Planner.App.Modules.Tasks.Entities;

public class TaskItem
{
    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public DateOnly Date { get; private set; }

    public TimeOnly? Time { get; private set; }

    public bool IsCompleted { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    private TaskItem()
    {
        Title = string.Empty;
    }

    public TaskItem(string title, DateOnly date, TimeOnly? time = null)
    {
        Id = Guid.NewGuid();
        IsCompleted = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        ApplyDetails(title, date, time);
    }

    private TaskItem(
        Guid id,
        string title,
        DateOnly date,
        TimeOnly? time,
        bool isCompleted,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        IsCompleted = isCompleted;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;

        ApplyDetails(title, date, time);
    }

    public static TaskItem Restore(
        Guid id,
        string title,
        DateOnly date,
        TimeOnly? time,
        bool isCompleted,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new TaskItem(id, title, date, time, isCompleted, createdAt, updatedAt);
    }

    public void Update(string title, DateOnly date, TimeOnly? time)
    {
        ApplyDetails(title, date, time);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        IsCompleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Uncomplete()
    {
        IsCompleted = false;
        UpdatedAt = DateTime.UtcNow;
    }

    private void ApplyDetails(string title, DateOnly date, TimeOnly? time)
    {
        Title = NormalizeTitle(title);
        Date = date;
        Time = time;
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty");

        return title.Trim();
    }
}

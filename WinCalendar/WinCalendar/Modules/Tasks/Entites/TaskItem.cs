using System;

namespace Planner.App.Modules.Tasks.Entities;

public class TaskItem
{
    public Guid Id { get; private set; }

    public string Title { get; private set; }

    public DateOnly Date { get; private set; }

    public TimeOnly? Time { get; private set; }

    public bool IsCompleted { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    private TaskItem() { }

    public TaskItem(string title, DateOnly date, TimeOnly? time = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty");

        Id = Guid.NewGuid();
        Title = title;
        Date = date;
        Time = time;
        IsCompleted = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string title, DateOnly date, TimeOnly? time)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty");

        Title = title;
        Date = date;
        Time = time;
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
}
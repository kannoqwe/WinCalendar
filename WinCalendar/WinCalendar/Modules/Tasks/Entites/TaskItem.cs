using System;

namespace Planner.App.Modules.Tasks.Entities;

public class TaskItem
{
    private const int MinimumDurationMinutes = 5;

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public DateOnly Date { get; private set; }

    public TimeOnly? Time { get; private set; }

    public int? DurationMinutes { get; private set; }

    public bool IsCompleted { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    private TaskItem()
    {
        Title = string.Empty;
    }

    public TaskItem(string title, DateOnly date, TimeOnly? time = null, int? durationMinutes = null)
    {
        Id = Guid.NewGuid();
        IsCompleted = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        ApplyDetails(title, date, time, durationMinutes);
    }

    private TaskItem(
        Guid id,
        string title,
        DateOnly date,
        TimeOnly? time,
        int? durationMinutes,
        bool isCompleted,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        IsCompleted = isCompleted;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;

        ApplyDetails(title, date, time, durationMinutes);
    }

    public static TaskItem Restore(
        Guid id,
        string title,
        DateOnly date,
        TimeOnly? time,
        int? durationMinutes,
        bool isCompleted,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new TaskItem(id, title, date, time, durationMinutes, isCompleted, createdAt, updatedAt);
    }

    public void Update(string title, DateOnly date, TimeOnly? time, int? durationMinutes)
    {
        ApplyDetails(title, date, time, durationMinutes);
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

    private void ApplyDetails(string title, DateOnly date, TimeOnly? time, int? durationMinutes)
    {
        Title = NormalizeTitle(title);
        Date = date;
        Time = time;
        DurationMinutes = NormalizeDurationMinutes(time, durationMinutes);
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty");

        return title.Trim();
    }

    private static int? NormalizeDurationMinutes(TimeOnly? time, int? durationMinutes)
    {
        if (time is null || durationMinutes is null)
            return null;

        if (durationMinutes < MinimumDurationMinutes)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), $"Duration must be at least {MinimumDurationMinutes} minutes.");

        int maxDurationMinutes = GetMaxDurationMinutes(time.Value);

        if (durationMinutes > maxDurationMinutes)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "Duration cannot extend past the end of the selected day.");

        return durationMinutes.Value;
    }

    private static int GetMaxDurationMinutes(TimeOnly time)
    {
        return (24 * 60) - (int)time.ToTimeSpan().TotalMinutes;
    }
}

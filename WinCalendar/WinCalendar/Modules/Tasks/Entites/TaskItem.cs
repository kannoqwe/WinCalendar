using System;

namespace Planner.App.Modules.Tasks.Entities;

public class TaskItem
{
    private const int MinimumDurationMinutes = 5;

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

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

    public TaskItem(
        string title,
        DateOnly date,
        DateTime createdAtUtc,
        TimeOnly? time = null,
        int? durationMinutes = null,
        string? description = null)
    {
        Id = Guid.NewGuid();
        IsCompleted = false;
        CreatedAt = NormalizeUtc(createdAtUtc);
        UpdatedAt = CreatedAt;

        ApplyDetails(title, date, time, durationMinutes, description);
    }

    private TaskItem(
        Guid id,
        string title,
        DateOnly date,
        TimeOnly? time,
        int? durationMinutes,
        string? description,
        bool isCompleted,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        IsCompleted = isCompleted;
        CreatedAt = NormalizeUtc(createdAt);
        UpdatedAt = NormalizeUtc(updatedAt);

        ApplyDetails(title, date, time, durationMinutes, description);
    }

    public static TaskItem Restore(
        Guid id,
        string title,
        DateOnly date,
        TimeOnly? time,
        int? durationMinutes,
        string? description,
        bool isCompleted,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new TaskItem(id, title, date, time, durationMinutes, description, isCompleted, createdAt, updatedAt);
    }

    public void Update(
        string title,
        DateOnly date,
        TimeOnly? time,
        int? durationMinutes,
        DateTime updatedAtUtc,
        string? description = null)
    {
        ApplyDetails(title, date, time, durationMinutes, description);
        UpdatedAt = NormalizeUtc(updatedAtUtc);
    }

    public void Complete(DateTime updatedAtUtc)
    {
        IsCompleted = true;
        UpdatedAt = NormalizeUtc(updatedAtUtc);
    }

    public void Uncomplete(DateTime updatedAtUtc)
    {
        IsCompleted = false;
        UpdatedAt = NormalizeUtc(updatedAtUtc);
    }

    private void ApplyDetails(string title, DateOnly date, TimeOnly? time, int? durationMinutes, string? description)
    {
        Title = NormalizeTitle(title);
        Description = NormalizeDescription(description);
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

    private static string NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? string.Empty
            : description.Trim();
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

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Planner.App.Modules.Tasks.Contracts;
using Planner.App.Modules.Tasks.Entities;
using Planner.App.Modules.Tasks.UseCases;
using WinCalendar.Core.Time;

try
{
    TaskItemTests.Run();
    await TaskUseCaseTests.RunAsync();
    Console.WriteLine("All tests passed.");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);
    return 1;
}

internal static class TaskItemTests
{
    public static void Run()
    {
        CreateTaskNormalizesTextAndTimestamps();
        TimedTaskCannotExtendPastEndOfDay();
        UpdateUsesExplicitTimestamp();
    }

    private static void CreateTaskNormalizesTextAndTimestamps()
    {
        DateTime createdAt = new(2026, 4, 17, 8, 30, 0, DateTimeKind.Utc);
        TaskItem task = new(
            "  Write plan  ",
            new DateOnly(2026, 4, 17),
            createdAt,
            description: "  Notes  ");

        Assert.Equal("Write plan", task.Title, "Task title should be trimmed.");
        Assert.Equal("Notes", task.Description, "Task description should be trimmed.");
        Assert.Equal(createdAt, task.CreatedAt, "CreatedAt should use the provided timestamp.");
        Assert.Equal(createdAt, task.UpdatedAt, "UpdatedAt should initially match CreatedAt.");
    }

    private static void TimedTaskCannotExtendPastEndOfDay()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TaskItem(
                "Late task",
                new DateOnly(2026, 4, 17),
                new DateTime(2026, 4, 17, 8, 0, 0, DateTimeKind.Utc),
                new TimeOnly(23, 50),
                durationMinutes: 15),
            "Task duration should not extend past the selected day.");
    }

    private static void UpdateUsesExplicitTimestamp()
    {
        TaskItem task = new(
            "Draft",
            new DateOnly(2026, 4, 17),
            new DateTime(2026, 4, 17, 8, 0, 0, DateTimeKind.Utc));
        DateTime updatedAt = new(2026, 4, 17, 9, 15, 0, DateTimeKind.Utc);

        task.Update("Final", new DateOnly(2026, 4, 18), null, null, updatedAt);

        Assert.Equal("Final", task.Title, "Updated title should be stored.");
        Assert.Equal(new DateOnly(2026, 4, 18), task.Date, "Updated date should be stored.");
        Assert.Equal(updatedAt, task.UpdatedAt, "UpdatedAt should use the provided timestamp.");
    }
}

internal static class TaskUseCaseTests
{
    public static async Task RunAsync()
    {
        await CreateTaskUsesClockTimestampAsync();
        await SetCompletionUsesClockTimestampAsync();
    }

    private static async Task CreateTaskUsesClockTimestampAsync()
    {
        FakeTaskRepository repository = new();
        FakeClock clock = new(new DateTime(2026, 4, 17, 10, 0, 0, DateTimeKind.Local));
        CreateTaskUseCase useCase = new(repository, clock);

        TaskItem task = await useCase.ExecuteAsync("Clocked", new DateOnly(2026, 4, 17));

        Assert.True(repository.AddedTasks.Contains(task), "Created task should be persisted.");
        Assert.Equal(clock.UtcNow, task.CreatedAt, "Created task should use clock UTC time.");
    }

    private static async Task SetCompletionUsesClockTimestampAsync()
    {
        DateTime createdAt = new(2026, 4, 17, 8, 0, 0, DateTimeKind.Utc);
        TaskItem task = new("Complete me", new DateOnly(2026, 4, 17), createdAt);
        FakeTaskRepository repository = new();
        repository.Seed(task);
        FakeClock clock = new(new DateTime(2026, 4, 17, 11, 0, 0, DateTimeKind.Local));
        SetTaskCompletionStatusUseCase useCase = new(repository, clock);

        await useCase.ExecuteAsync(task.Id, true);

        Assert.True(task.IsCompleted, "Task should be completed.");
        Assert.Equal(clock.UtcNow, task.UpdatedAt, "Completion should use clock UTC time.");
        Assert.True(repository.UpdatedTasks.Contains(task), "Completed task should be persisted.");
    }
}

internal sealed class FakeClock : IClock
{
    public FakeClock(DateTime now)
    {
        Now = now;
    }

    public DateTime Now { get; }

    public DateTime UtcNow => Now.ToUniversalTime();

    public DateOnly Today => DateOnly.FromDateTime(Now);

    public TimeOnly TimeOfDay => TimeOnly.FromDateTime(Now);
}

internal sealed class FakeTaskRepository : ITaskRepository
{
    private readonly Dictionary<Guid, TaskItem> _tasks = [];

    public List<TaskItem> AddedTasks { get; } = [];

    public List<TaskItem> UpdatedTasks { get; } = [];

    public void Seed(TaskItem task)
    {
        _tasks[task.Id] = task;
    }

    public Task<IReadOnlyList<TaskItem>> GetByDateAsync(DateOnly date)
    {
        IReadOnlyList<TaskItem> tasks = _tasks.Values.Where(task => task.Date == date).ToList();
        return Task.FromResult(tasks);
    }

    public Task<IReadOnlyList<TaskItem>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate)
    {
        IReadOnlyList<TaskItem> tasks = _tasks.Values
            .Where(task => task.Date >= startDate && task.Date <= endDate)
            .ToList();
        return Task.FromResult(tasks);
    }

    public Task<IReadOnlyList<TaskItem>> GetIncompleteTimedTasksDueBeforeAsync(DateOnly date, TimeOnly time)
    {
        IReadOnlyList<TaskItem> tasks = _tasks.Values
            .Where(task => !task.IsCompleted && task.Time is not null && task.Date <= date)
            .ToList();
        return Task.FromResult(tasks);
    }

    public Task<TaskItem?> GetByIdAsync(Guid id)
    {
        _tasks.TryGetValue(id, out TaskItem? task);
        return Task.FromResult(task);
    }

    public Task AddAsync(TaskItem task)
    {
        _tasks[task.Id] = task;
        AddedTasks.Add(task);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TaskItem task)
    {
        _tasks[task.Id] = task;
        UpdatedTasks.Add(task);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _tasks.Remove(id);
        return Task.CompletedTask;
    }
}

internal static class Assert
{
    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{message} Expected '{expected}', actual '{actual}'.");
    }

    public static void True(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    public static void Throws<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }
}

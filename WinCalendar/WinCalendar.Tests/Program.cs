using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Planner.App.Modules.Tasks.Contracts;
using Planner.App.Modules.Tasks.Entities;
using Planner.App.Modules.Tasks.Infrastructure.Sqlite;
using Planner.App.Modules.Tasks.UseCases;
using WinCalendar.Core.Time;
using WinCalendar.Modules.Planner.Presentation;

try
{
    TaskItemTests.Run();
    PlannerQuickTaskParserTests.Run();
    await TaskUseCaseTests.RunAsync();
    await SqliteTaskRepositoryTests.RunAsync();
    Console.WriteLine("All tests passed.");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);
    return 1;
}

internal static class PlannerQuickTaskParserTests
{
    public static void Run()
    {
        ParsesEnglishQuickTask();
        ParsesRussianQuickTask();
    }

    private static void ParsesEnglishQuickTask()
    {
        PlannerQuickTaskParseResult result = PlannerQuickTaskParser.Parse(
            "meeting important tomorrow 14:30 45m weekly",
            new DateOnly(2026, 4, 24),
            new DateOnly(2026, 4, 24))!;

        Assert.Equal("meeting", result.Title, "Quick parser should remove recognized English tokens.");
        Assert.Equal(new DateOnly(2026, 4, 25), result.Date, "Quick parser should understand tomorrow.");
        Assert.Equal(new TimeOnly(14, 30), result.Time, "Quick parser should understand time.");
        Assert.Equal(45, result.DurationMinutes, "Quick parser should understand minute duration.");
        Assert.Equal(TaskRecurrencePattern.Weekly, result.RecurrencePattern, "Quick parser should understand recurrence.");
        Assert.Equal(TaskCategory.Important, result.Category, "Quick parser should understand category.");
    }

    private static void ParsesRussianQuickTask()
    {
        PlannerQuickTaskParseResult result = PlannerQuickTaskParser.Parse(
            "созвон завтра 9:15 1ч еженедельно",
            new DateOnly(2026, 4, 24),
            new DateOnly(2026, 4, 24))!;

        Assert.Equal("созвон", result.Title, "Quick parser should remove recognized Russian tokens.");
        Assert.Equal(new DateOnly(2026, 4, 25), result.Date, "Quick parser should understand Russian tomorrow.");
        Assert.Equal(new TimeOnly(9, 15), result.Time, "Quick parser should understand Russian time.");
        Assert.Equal(60, result.DurationMinutes, "Quick parser should understand Russian hour duration.");
        Assert.Equal(TaskRecurrencePattern.Weekly, result.RecurrencePattern, "Quick parser should understand Russian recurrence.");
        Assert.Equal(TaskCategory.Work, result.Category, "Quick parser should default to work category.");
    }
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
        Assert.Equal(TaskCategory.Work, task.Category, "Task category should default to work.");
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
        await CreateTaskMaterializesRecurringTasksAsync();
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

    private static async Task CreateTaskMaterializesRecurringTasksAsync()
    {
        FakeTaskRepository repository = new();
        FakeClock clock = new(new DateTime(2026, 4, 17, 10, 0, 0, DateTimeKind.Utc));
        CreateTaskUseCase useCase = new(repository, clock);

        TaskItem task = await useCase.ExecuteAsync(
            "Standup",
            new DateOnly(2026, 4, 17),
            recurrencePattern: TaskRecurrencePattern.Weekly);

        Assert.Equal(TaskRecurrencePattern.Weekly, task.RecurrencePattern, "Source task should store recurrence.");
        Assert.Equal(13, repository.AddedTasks.Count, "Weekly recurrence should create the source plus 12 future tasks.");
        Assert.True(
            repository.AddedTasks.Any(current => current.Date == new DateOnly(2026, 4, 24)),
            "Weekly recurrence should create the next week occurrence.");
    }
}

internal static class SqliteTaskRepositoryTests
{
    public static async Task RunAsync()
    {
        await PersistAndQueryTaskAsync();
        await UpdateAndDeleteTaskAsync();
    }

    private static async Task PersistAndQueryTaskAsync()
    {
        string databasePath = CreateDatabasePath();

        try
        {
            TaskDatabaseInitializer initializer = new(databasePath);
            initializer.Initialize();
            SqliteTaskRepository repository = new(databasePath);
            TaskItem task = new(
                "Repository task",
                new DateOnly(2026, 4, 24),
                new DateTime(2026, 4, 24, 8, 0, 0, DateTimeKind.Utc),
                new TimeOnly(14, 30),
                45,
                "Stored note");

            await repository.AddAsync(task);

            IReadOnlyList<TaskItem> tasks = await repository.GetByDateRangeAsync(
                new DateOnly(2026, 4, 24),
                new DateOnly(2026, 4, 24));

            TaskItem restored = Assert.Single(tasks, "Repository should return the persisted task.");
            Assert.Equal(task.Id, restored.Id, "Task id should round-trip.");
            Assert.Equal("Repository task", restored.Title, "Task title should round-trip.");
            Assert.Equal("Stored note", restored.Description, "Task description should round-trip.");
            Assert.Equal(new TimeOnly(14, 30), restored.Time, "Task time should round-trip.");
            Assert.Equal(45, restored.DurationMinutes, "Task duration should round-trip.");
            Assert.Equal(TaskRecurrencePattern.None, restored.RecurrencePattern, "Default recurrence should round-trip.");
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    private static async Task UpdateAndDeleteTaskAsync()
    {
        string databasePath = CreateDatabasePath();

        try
        {
            TaskDatabaseInitializer initializer = new(databasePath);
            initializer.Initialize();
            SqliteTaskRepository repository = new(databasePath);
            TaskItem task = new(
                "Draft",
                new DateOnly(2026, 4, 24),
                new DateTime(2026, 4, 24, 8, 0, 0, DateTimeKind.Utc));

            await repository.AddAsync(task);
            task.Update(
                "Final",
                new DateOnly(2026, 4, 25),
                new TimeOnly(9, 15),
                30,
                new DateTime(2026, 4, 24, 9, 0, 0, DateTimeKind.Utc),
                "Updated",
                TaskRecurrencePattern.Daily,
                TaskCategory.Important);
            await repository.UpdateAsync(task);

            TaskItem? restored = await repository.GetByIdAsync(task.Id);
            Assert.NotNull(restored, "Updated task should still exist.");
            Assert.Equal("Final", restored!.Title, "Updated title should be stored.");
            Assert.Equal(new DateOnly(2026, 4, 25), restored.Date, "Updated date should be stored.");
            Assert.Equal(new TimeOnly(9, 15), restored.Time, "Updated time should be stored.");
            Assert.Equal(30, restored.DurationMinutes, "Updated duration should be stored.");
            Assert.Equal(TaskRecurrencePattern.Daily, restored.RecurrencePattern, "Updated recurrence should be stored.");
            Assert.Equal(TaskCategory.Important, restored.Category, "Updated category should be stored.");

            await repository.DeleteAsync(task.Id);

            TaskItem? deleted = await repository.GetByIdAsync(task.Id);
            Assert.Null(deleted, "Deleted task should not be returned.");
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    private static string CreateDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), $"WinCalendar.Tests.{Guid.NewGuid():N}.db");
    }

    private static void DeleteDatabase(string databasePath)
    {
        foreach (string path in new[] { databasePath, $"{databasePath}-wal", $"{databasePath}-shm" })
        {
            if (File.Exists(path))
                File.Delete(path);
        }
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

    public static void Null<T>(T? actual, string message)
    {
        if (actual is not null)
            throw new InvalidOperationException(message);
    }

    public static void NotNull<T>(T? actual, string message)
    {
        if (actual is null)
            throw new InvalidOperationException(message);
    }

    public static T Single<T>(IReadOnlyList<T> values, string message)
    {
        if (values.Count != 1)
            throw new InvalidOperationException(message);

        return values[0];
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

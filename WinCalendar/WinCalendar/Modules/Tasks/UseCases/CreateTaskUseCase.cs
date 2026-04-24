using Planner.App.Modules.Tasks.Contracts;
using Planner.App.Modules.Tasks.Entities;
using System;
using System.Threading.Tasks;
using WinCalendar.Core.Time;

namespace Planner.App.Modules.Tasks.UseCases;

public sealed class CreateTaskUseCase
{
    private readonly ITaskRepository _taskRepository;
    private readonly IClock _clock;

    public CreateTaskUseCase(ITaskRepository taskRepository, IClock clock)
    {
        _taskRepository = taskRepository;
        _clock = clock;
    }

    public async Task<TaskItem> ExecuteAsync(
        string title,
        DateOnly date,
        TimeOnly? time = null,
        int? durationMinutes = null,
        string? description = null,
        TaskRecurrencePattern recurrencePattern = TaskRecurrencePattern.None,
        TaskCategory category = TaskCategory.Work)
    {
        TaskItem task = new(title, date, _clock.UtcNow, time, durationMinutes, description, recurrencePattern, category);

        await _taskRepository.AddAsync(task);
        await CreateFutureOccurrencesAsync(task);

        return task;
    }

    private async Task CreateFutureOccurrencesAsync(TaskItem sourceTask)
    {
        int occurrenceCount = sourceTask.RecurrencePattern switch
        {
            TaskRecurrencePattern.Daily => 30,
            TaskRecurrencePattern.Weekly => 12,
            TaskRecurrencePattern.Monthly => 6,
            _ => 0
        };

        for (int index = 1; index <= occurrenceCount; index++)
        {
            DateOnly occurrenceDate = sourceTask.RecurrencePattern switch
            {
                TaskRecurrencePattern.Daily => sourceTask.Date.AddDays(index),
                TaskRecurrencePattern.Weekly => sourceTask.Date.AddDays(index * 7),
                TaskRecurrencePattern.Monthly => sourceTask.Date.AddMonths(index),
                _ => sourceTask.Date
            };

            TaskItem occurrence = new(
                sourceTask.Title,
                occurrenceDate,
                _clock.UtcNow,
                sourceTask.Time,
                sourceTask.DurationMinutes,
                sourceTask.Description,
                sourceTask.RecurrencePattern,
                sourceTask.Category);

            await _taskRepository.AddAsync(occurrence);
        }
    }
}

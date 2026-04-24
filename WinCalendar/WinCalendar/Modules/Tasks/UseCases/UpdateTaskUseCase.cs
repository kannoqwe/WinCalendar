using Planner.App.Modules.Tasks.Contracts;
using Planner.App.Modules.Tasks.Entities;
using System;
using System.Threading.Tasks;
using WinCalendar.Core.Time;

namespace Planner.App.Modules.Tasks.UseCases;

public sealed class UpdateTaskUseCase
{
    private readonly ITaskRepository _taskRepository;
    private readonly IClock _clock;

    public UpdateTaskUseCase(ITaskRepository taskRepository, IClock clock)
    {
        _taskRepository = taskRepository;
        _clock = clock;
    }

    public async Task<TaskItem> ExecuteAsync(
        Guid id,
        string title,
        DateOnly date,
        TimeOnly? time = null,
        int? durationMinutes = null,
        string? description = null,
        TaskRecurrencePattern recurrencePattern = TaskRecurrencePattern.None)
    {
        TaskItem task = await _taskRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"Task with id '{id}' was not found.");

        task.Update(title, date, time, durationMinutes, _clock.UtcNow, description, recurrencePattern);

        await _taskRepository.UpdateAsync(task);

        return task;
    }
}

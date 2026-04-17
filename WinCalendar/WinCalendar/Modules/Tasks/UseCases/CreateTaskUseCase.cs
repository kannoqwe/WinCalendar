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
        string? description = null)
    {
        TaskItem task = new(title, date, _clock.UtcNow, time, durationMinutes, description);

        await _taskRepository.AddAsync(task);

        return task;
    }
}

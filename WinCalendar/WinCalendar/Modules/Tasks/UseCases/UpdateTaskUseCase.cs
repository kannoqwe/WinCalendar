using Planner.App.Modules.Tasks.Contracts;
using Planner.App.Modules.Tasks.Entities;
using System;
using System.Threading.Tasks;

namespace Planner.App.Modules.Tasks.UseCases;

public sealed class UpdateTaskUseCase
{
    private readonly ITaskRepository _taskRepository;

    public UpdateTaskUseCase(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<TaskItem> ExecuteAsync(
        Guid id,
        string title,
        DateOnly date,
        TimeOnly? time = null,
        int? durationMinutes = null,
        string? description = null)
    {
        TaskItem task = await _taskRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"Task with id '{id}' was not found.");

        task.Update(title, date, time, durationMinutes, description);

        await _taskRepository.UpdateAsync(task);

        return task;
    }
}

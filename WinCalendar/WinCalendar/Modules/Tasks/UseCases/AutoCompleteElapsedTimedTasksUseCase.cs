using Planner.App.Modules.Tasks.Contracts;
using Planner.App.Modules.Tasks.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planner.App.Modules.Tasks.UseCases;

public sealed class AutoCompleteElapsedTimedTasksUseCase
{
    private readonly ITaskRepository _taskRepository;

    public AutoCompleteElapsedTimedTasksUseCase(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<bool> ExecuteAsync(DateTime now)
    {
        IReadOnlyList<TaskItem> dueTasks = await _taskRepository.GetIncompleteTimedTasksDueBeforeAsync(
            DateOnly.FromDateTime(now),
            TimeOnly.FromDateTime(now));

        if (dueTasks.Count == 0)
            return false;

        foreach (TaskItem task in dueTasks)
        {
            task.Complete();
            await _taskRepository.UpdateAsync(task);
        }

        return true;
    }
}

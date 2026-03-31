using Planner.App.Modules.Tasks.Contracts;
using Planner.App.Modules.Tasks.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planner.App.Modules.Tasks.UseCases;

public sealed class GetTasksForDayUseCase
{
    private readonly ITaskRepository _taskRepository;

    public GetTasksForDayUseCase(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public Task<IReadOnlyList<TaskItem>> ExecuteAsync(DateOnly date)
    {
        return _taskRepository.GetByDateAsync(date);
    }
}
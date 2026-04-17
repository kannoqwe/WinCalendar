using Planner.App.Modules.Tasks.Contracts;
using Planner.App.Modules.Tasks.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planner.App.Modules.Tasks.UseCases;

public sealed class GetTasksForRangeUseCase
{
    private readonly ITaskRepository _taskRepository;

    public GetTasksForRangeUseCase(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public Task<IReadOnlyList<TaskItem>> ExecuteAsync(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
            throw new ArgumentException("End date must be greater than or equal to start date.");

        return _taskRepository.GetByDateRangeAsync(startDate, endDate);
    }
}

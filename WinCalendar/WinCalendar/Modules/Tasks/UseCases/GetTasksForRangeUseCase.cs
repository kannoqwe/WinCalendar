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

    public async Task<IReadOnlyList<TaskItem>> ExecuteAsync(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
            throw new ArgumentException("End date must be greater than or equal to start date.");

        List<TaskItem> tasks = [];

        for (DateOnly date = startDate; date <= endDate; date = date.AddDays(1))
        {
            IReadOnlyList<TaskItem> tasksForDay = await _taskRepository.GetByDateAsync(date);

            foreach (TaskItem task in tasksForDay)
                tasks.Add(task);
        }

        return tasks;
    }
}

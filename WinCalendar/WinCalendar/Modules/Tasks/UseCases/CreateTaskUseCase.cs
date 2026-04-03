using Planner.App.Modules.Tasks.Contracts;
using Planner.App.Modules.Tasks.Entities;
using System;
using System.Threading.Tasks;

namespace Planner.App.Modules.Tasks.UseCases;

public sealed class CreateTaskUseCase
{
    private readonly ITaskRepository _taskRepository;

    public CreateTaskUseCase(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<TaskItem> ExecuteAsync(string title, DateOnly date, TimeOnly? time = null, int? durationMinutes = null)
    {
        TaskItem task = new TaskItem(title, date, time, durationMinutes);

        await _taskRepository.AddAsync(task);

        return task;
    }
}

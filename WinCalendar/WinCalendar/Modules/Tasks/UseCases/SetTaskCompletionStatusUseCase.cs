using Planner.App.Modules.Tasks.Contracts;
using Planner.App.Modules.Tasks.Entities;
using System;
using System.Threading.Tasks;

namespace Planner.App.Modules.Tasks.UseCases;

public sealed class SetTaskCompletionStatusUseCase
{
    private readonly ITaskRepository _taskRepository;

    public SetTaskCompletionStatusUseCase(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task ExecuteAsync(Guid id, bool isCompleted)
    {
        TaskItem task = await _taskRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"Task with id '{id}' was not found.");

        if (task.IsCompleted == isCompleted)
            return;

        if (isCompleted)
            task.Complete();
        else
            task.Uncomplete();

        await _taskRepository.UpdateAsync(task);
    }
}

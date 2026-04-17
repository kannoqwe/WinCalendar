using Planner.App.Modules.Tasks.Contracts;
using Planner.App.Modules.Tasks.Entities;
using System;
using System.Threading.Tasks;
using WinCalendar.Core.Time;

namespace Planner.App.Modules.Tasks.UseCases;

public sealed class SetTaskCompletionStatusUseCase
{
    private readonly ITaskRepository _taskRepository;
    private readonly IClock _clock;

    public SetTaskCompletionStatusUseCase(ITaskRepository taskRepository, IClock clock)
    {
        _taskRepository = taskRepository;
        _clock = clock;
    }

    public async Task ExecuteAsync(Guid id, bool isCompleted)
    {
        TaskItem task = await _taskRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"Task with id '{id}' was not found.");

        if (task.IsCompleted == isCompleted)
            return;

        if (isCompleted)
            task.Complete(_clock.UtcNow);
        else
            task.Uncomplete(_clock.UtcNow);

        await _taskRepository.UpdateAsync(task);
    }
}

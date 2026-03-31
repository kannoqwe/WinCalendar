using Planner.App.Modules.Tasks.Contracts;
using System;
using System.Threading.Tasks;

namespace Planner.App.Modules.Tasks.UseCases;

public sealed class DeleteTaskUseCase
{
    private readonly ITaskRepository _taskRepository;

    public DeleteTaskUseCase(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public Task ExecuteAsync(Guid id)
    {
        return _taskRepository.DeleteAsync(id);
    }
}

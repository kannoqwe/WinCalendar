using Planner.App.Modules.Tasks.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planner.App.Modules.Tasks.Contracts;

public interface ITaskRepository
{
    Task<IReadOnlyList<TaskItem>> GetByDateAsync(DateOnly date);
    Task<IReadOnlyList<TaskItem>> GetIncompleteTimedTasksDueBeforeAsync(DateOnly date, TimeOnly time);
    Task<TaskItem?> GetByIdAsync(Guid id);
    Task AddAsync(TaskItem task);
    Task UpdateAsync(TaskItem task);
    Task DeleteAsync(Guid id);
}

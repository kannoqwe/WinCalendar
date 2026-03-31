using Planner.App.Modules.Tasks.Contracts;
using Planner.App.Modules.Tasks.Infrastructure.Sqlite;
using Planner.App.Modules.Tasks.UseCases;
using WinCalendar.Modules.Planner.Presentation;
using WinCalendar.Shared.Windowing;

namespace WinCalendar.Bootstrap;

public sealed class AppBootstrapper
{
    public MainWindow CreateMainWindow()
    {
        string databasePath = AppPaths.GetDatabasePath();

        TaskDatabaseInitializer databaseInitializer = new(databasePath);
        databaseInitializer.Initialize();

        ITaskRepository taskRepository = new SqliteTaskRepository(databasePath);

        CreateTaskUseCase createTaskUseCase = new(taskRepository);
        GetTasksForRangeUseCase getTasksForRangeUseCase = new(taskRepository);
        SetTaskCompletionStatusUseCase setTaskCompletionStatusUseCase = new(taskRepository);
        DeleteTaskUseCase deleteTaskUseCase = new(taskRepository);
        UpdateTaskUseCase updateTaskUseCase = new(taskRepository);

        PlannerStateStore plannerStateStore = new(
            createTaskUseCase,
            getTasksForRangeUseCase,
            setTaskCompletionStatusUseCase,
            deleteTaskUseCase,
            updateTaskUseCase);

        PlannerWindowCoordinator plannerWindowCoordinator = new(plannerStateStore);
        MainWindow mainWindow = new(plannerStateStore, plannerWindowCoordinator);
        plannerWindowCoordinator.AttachMainWindow(mainWindow);

        return mainWindow;
    }
}

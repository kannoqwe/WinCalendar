using Planner.App.Modules.Tasks.Contracts;
using Planner.App.Modules.Tasks.Infrastructure.Sqlite;
using Planner.App.Modules.Tasks.UseCases;
using WinCalendar.Modules.Planner.Presentation;
using WinCalendar.Shared.Windowing;

namespace WinCalendar.Bootstrap;

public sealed class AppBootstrapper
{
    private readonly PlannerStateStore _plannerStateStore;
    private readonly PlannerWindowCoordinator _plannerWindowCoordinator;

    public AppBootstrapper()
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

        _plannerStateStore = new(
            createTaskUseCase,
            getTasksForRangeUseCase,
            setTaskCompletionStatusUseCase,
            deleteTaskUseCase,
            updateTaskUseCase);

        _plannerWindowCoordinator = new(_plannerStateStore, CreateMainWindow);
    }

    public MainWindow CreateMainWindow()
    {
        MainWindow mainWindow = new(_plannerStateStore, _plannerWindowCoordinator);
        _plannerWindowCoordinator.AttachMainWindow(mainWindow);

        return mainWindow;
    }
}

using System;
using Planner.App.Modules.Tasks.Contracts;
using Planner.App.Modules.Tasks.Infrastructure.Sqlite;
using Planner.App.Modules.Tasks.UseCases;
using WinCalendar.Modules.Shell.UI;
using WinCalendar.Modules.Planner.Presentation;
using WinCalendar.Shared.Windowing;

namespace WinCalendar.Bootstrap;

public sealed class AppBootstrapper
{
    private readonly PlannerStateStore _plannerStateStore;
    private readonly PlannerWindowCoordinator _plannerWindowCoordinator;
    private readonly ShellExperienceCoordinator _shellExperienceCoordinator;

    public AppBootstrapper(Action requestExit)
    {
        string databasePath = AppPaths.GetDatabasePath();

        TaskDatabaseInitializer databaseInitializer = new(databasePath);
        databaseInitializer.Initialize();

        ITaskRepository taskRepository = new SqliteTaskRepository(databasePath);

        CreateTaskUseCase createTaskUseCase = new(taskRepository);
        GetTasksForRangeUseCase getTasksForRangeUseCase = new(taskRepository);
        AutoCompleteElapsedTimedTasksUseCase autoCompleteElapsedTimedTasksUseCase = new(taskRepository);
        SetTaskCompletionStatusUseCase setTaskCompletionStatusUseCase = new(taskRepository);
        DeleteTaskUseCase deleteTaskUseCase = new(taskRepository);
        UpdateTaskUseCase updateTaskUseCase = new(taskRepository);

        _plannerStateStore = new(
            createTaskUseCase,
            getTasksForRangeUseCase,
            autoCompleteElapsedTimedTasksUseCase,
            setTaskCompletionStatusUseCase,
            deleteTaskUseCase,
            updateTaskUseCase);

        _plannerWindowCoordinator = new(_plannerStateStore, CreateMainWindow);
        _shellExperienceCoordinator = new(_plannerWindowCoordinator, requestExit);
    }

    public MainWindow CreateMainWindow()
    {
        MainWindow mainWindow = new(_plannerStateStore, _plannerWindowCoordinator);
        _plannerWindowCoordinator.AttachMainWindow(mainWindow);

        return mainWindow;
    }

    public void Launch(AppLaunchMode launchMode)
    {
        _shellExperienceCoordinator.Start();

        switch (launchMode)
        {
            case AppLaunchMode.BackgroundShell:
                return;
            case AppLaunchMode.CompactPanel:
                _plannerWindowCoordinator.ShowCompactPanel();
                return;
            default:
                _plannerWindowCoordinator.ShowFullApp();
                return;
        }
    }

    public void Shutdown()
    {
        _shellExperienceCoordinator.Dispose();
    }
}

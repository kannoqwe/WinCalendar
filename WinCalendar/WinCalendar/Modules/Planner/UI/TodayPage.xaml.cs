using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCalendar.Modules.Planner.Presentation;
using WinCalendar.Shared.Windowing;

namespace WinCalendar.Modules.Planner.UI;

public sealed partial class TodayPage : Page
{
    private readonly PlannerStateStore _plannerStateStore;
    private readonly PlannerWindowCoordinator _windowCoordinator;
    private bool _initialized;

    public TodayPage(PlannerStateStore plannerStateStore, PlannerWindowCoordinator windowCoordinator)
    {
        _plannerStateStore = plannerStateStore;
        _windowCoordinator = windowCoordinator;
        InitializeComponent();
        DataContext = _plannerStateStore;
    }

    private async void Root_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
            return;

        _initialized = true;
        await _plannerStateStore.EnsureInitializedAsync();
    }

    private async void PreviousWeekButton_Click(object sender, RoutedEventArgs e)
    {
        await _plannerStateStore.GoToPreviousWeekAsync();
    }

    private async void CurrentWeekButton_Click(object sender, RoutedEventArgs e)
    {
        await _plannerStateStore.GoToTodayAsync();
    }

    private async void NextWeekButton_Click(object sender, RoutedEventArgs e)
    {
        await _plannerStateStore.GoToNextWeekAsync();
    }

    private async void WeekDayHeaderButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlannerWeekDayTimelineViewModel day })
            return;

        await _plannerStateStore.SelectDateAsync(day.Date);
    }

    private async void WeekAllDayTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlannerTaskViewModel task })
            return;

        await OpenTaskAsync(task);
    }

    private async void WeekTimedTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlannerWeekTaskBlockViewModel taskBlock })
            return;

        await OpenTaskAsync(taskBlock.Task);
    }

    private async void AddTaskButton_Click(object sender, RoutedEventArgs e)
    {
        await TaskComposerDialogService.ShowAddTaskAsync(XamlRoot, _plannerStateStore, _plannerStateStore.SelectedDate);
    }

    private async Task OpenTaskAsync(PlannerTaskViewModel task)
    {
        await _plannerStateStore.SelectDateAsync(task.Date);
        PlannerTaskViewModel selectedTask = _plannerStateStore.SelectedDayTasks
            .FirstOrDefault(current => current.Id == task.Id)
            ?? task;

        _plannerStateStore.SelectTask(selectedTask);
        _windowCoordinator.ShowMediumView();
    }
}

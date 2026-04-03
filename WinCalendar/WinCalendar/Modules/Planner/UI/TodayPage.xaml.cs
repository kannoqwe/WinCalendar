using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCalendar.Modules.Planner.Presentation;
using WinCalendar.Shared.Windowing;

namespace WinCalendar.Modules.Planner.UI;

public sealed partial class TodayPage : Page
{
    private readonly PlannerStateStore _plannerStateStore;
    private readonly PlannerWindowCoordinator _windowCoordinator;
    private readonly DispatcherQueueTimer _currentTimeTimer;
    private bool _initialized;

    public TodayPage(PlannerStateStore plannerStateStore, PlannerWindowCoordinator windowCoordinator)
    {
        _plannerStateStore = plannerStateStore;
        _windowCoordinator = windowCoordinator;
        InitializeComponent();
        DataContext = _plannerStateStore;
        _currentTimeTimer = DispatcherQueue.CreateTimer();
        _currentTimeTimer.Interval = TimeSpan.FromMinutes(1);
        _currentTimeTimer.IsRepeating = true;
        _currentTimeTimer.Tick += CurrentTimeTimer_Tick;
        Unloaded += TodayPage_Unloaded;
    }

    private async void Root_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
        {
            StartCurrentTimeTimer();
            return;
        }

        _initialized = true;
        await _plannerStateStore.EnsureInitializedAsync();
        _plannerStateStore.RefreshCurrentTimeIndicator();
        StartCurrentTimeTimer();
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

    private void CurrentTimeTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        _plannerStateStore.RefreshCurrentTimeIndicator();
    }

    private void TodayPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _currentTimeTimer.Stop();
    }

    private void StartCurrentTimeTimer()
    {
        _plannerStateStore.RefreshCurrentTimeIndicator();

        if (_currentTimeTimer.IsRunning)
            return;

        _currentTimeTimer.Start();
    }
}

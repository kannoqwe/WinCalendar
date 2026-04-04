using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinCalendar.Modules.Planner.Presentation;
using WinCalendar.Shared.Windowing;

namespace WinCalendar.Modules.Planner.UI;

public sealed partial class TodayPage : Page
{
    private const int TimelineSnapMinutes = 15;
    private readonly PlannerStateStore _plannerStateStore;
    private readonly DispatcherQueueTimer _currentTimeTimer;
    private bool _initialized;

    public TodayPage(PlannerStateStore plannerStateStore, PlannerWindowCoordinator windowCoordinator)
    {
        _plannerStateStore = plannerStateStore;
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

    private void AddTaskButton_Click(object sender, RoutedEventArgs e)
    {
        _plannerStateStore.BeginNewTaskDraft(_plannerStateStore.SelectedDate);
    }

    private async Task OpenTaskAsync(PlannerTaskViewModel task)
    {
        await _plannerStateStore.SelectDateAsync(task.Date);
        PlannerTaskViewModel selectedTask = _plannerStateStore.SelectedDayTasks
            .FirstOrDefault(current => current.Id == task.Id)
            ?? task;

        _plannerStateStore.SelectTask(selectedTask);
    }

    private async void WeekAllDayLane_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PlannerWeekDayTimelineViewModel day })
            return;

        await _plannerStateStore.SelectDateAsync(day.Date);
        _plannerStateStore.BeginNewTaskDraft(day.Date);
    }

    private async void WeekTimedGrid_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PlannerWeekDayTimelineViewModel day } surface)
            return;

        await _plannerStateStore.SelectDateAsync(day.Date);
        var clickPosition = e.GetPosition(surface);
        TimeOnly time = GetTimeFromTimelinePosition(clickPosition.Y);
        _plannerStateStore.BeginNewTaskDraft(day.Date, time);
    }

    private async void SaveTaskButton_Click(object sender, RoutedEventArgs e)
    {
        await _plannerStateStore.SaveSelectedTaskAsync();
    }

    private async void CompleteTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (_plannerStateStore.SelectedTask is null)
            return;

        await _plannerStateStore.ToggleTaskCompletionAsync(
            _plannerStateStore.SelectedTask.Id,
            !_plannerStateStore.SelectedTask.IsCompleted);
    }

    private async void DeleteSelectedTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (_plannerStateStore.SelectedTask is null)
            return;

        await _plannerStateStore.DeleteTaskAsync(_plannerStateStore.SelectedTask.Id);
    }

    private void CloseEditorButton_Click(object sender, RoutedEventArgs e)
    {
        _plannerStateStore.SelectTask(null);
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

    private TimeOnly GetTimeFromTimelinePosition(double offsetY)
    {
        double rawMinutes = Math.Max(0d, (offsetY / _plannerStateStore.WeekTimelineHourHeight) * 60d);
        int roundedMinutes = (int)(Math.Round(rawMinutes / TimelineSnapMinutes) * TimelineSnapMinutes);
        roundedMinutes = Math.Clamp(roundedMinutes, 0, (24 * 60) - 5);

        return new TimeOnly(roundedMinutes / 60, roundedMinutes % 60);
    }
}

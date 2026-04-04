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
    private readonly DispatcherQueueTimer _inlineSaveTimer;
    private bool _timersInitialized;
    private bool _suppressInlineSave;
    private bool _isInlineSaveInProgress;
    private bool _inlineSaveQueued;
    private bool _focusTitleEditorAfterSelection;
    private bool _initialized;

    public TodayPage(PlannerStateStore plannerStateStore, PlannerWindowCoordinator windowCoordinator)
    {
        _plannerStateStore = plannerStateStore;
        _currentTimeTimer = DispatcherQueue.CreateTimer();
        _currentTimeTimer.Interval = TimeSpan.FromMinutes(1);
        _currentTimeTimer.IsRepeating = true;
        _currentTimeTimer.Tick += CurrentTimeTimer_Tick;
        _inlineSaveTimer = DispatcherQueue.CreateTimer();
        _inlineSaveTimer.Interval = TimeSpan.FromMilliseconds(350);
        _inlineSaveTimer.IsRepeating = false;
        _inlineSaveTimer.Tick += InlineSaveTimer_Tick;
        _timersInitialized = true;
        InitializeComponent();
        DataContext = _plannerStateStore;
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
        SuspendInlineEditor();
        _plannerStateStore.BeginNewTaskDraft(_plannerStateStore.SelectedDate);
        ResumeInlineEditor(focusTitleEditor: true);
    }

    private async Task OpenTaskAsync(PlannerTaskViewModel task)
    {
        SuspendInlineEditor();
        await _plannerStateStore.SelectDateAsync(task.Date);
        PlannerTaskViewModel selectedTask = _plannerStateStore.SelectedDayTasks
            .FirstOrDefault(current => current.Id == task.Id)
            ?? task;

        _plannerStateStore.SelectTask(selectedTask);
        ResumeInlineEditor(focusTitleEditor: false);
    }

    private async void WeekAllDayLane_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PlannerWeekDayTimelineViewModel day })
            return;

        SuspendInlineEditor();
        await _plannerStateStore.SelectDateAsync(day.Date);
        _plannerStateStore.BeginNewTaskDraft(day.Date);
        ResumeInlineEditor(focusTitleEditor: true);
    }

    private async void WeekTimedGrid_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PlannerWeekDayTimelineViewModel day } surface)
            return;

        SuspendInlineEditor();
        await _plannerStateStore.SelectDateAsync(day.Date);
        var clickPosition = e.GetPosition(surface);
        TimeOnly time = GetTimeFromTimelinePosition(clickPosition.Y);
        _plannerStateStore.BeginNewTaskDraft(day.Date, time);
        ResumeInlineEditor(focusTitleEditor: true);
    }

    private async void EditorDatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        await SaveInlineEditorNowAsync();
    }

    private async void EditorTitleTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        await SaveInlineEditorNowAsync();
    }

    private void EditorTitleTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ScheduleInlineSave();
    }

    private void EditorTimeToggle_Click(object sender, RoutedEventArgs e)
    {
        ScheduleInlineSave();
    }

    private void EditorDurationToggle_Click(object sender, RoutedEventArgs e)
    {
        ScheduleInlineSave();
    }

    private void EditorTimePicker_TimeChanged(object sender, TimePickerValueChangedEventArgs args)
    {
        ScheduleInlineSave();
    }

    private void EditorDurationNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        ScheduleInlineSave();
    }

    private async void InlineSaveTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        await SaveInlineEditorNowAsync();
    }

    private async void CompleteTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (_plannerStateStore.SelectedTask is null)
            return;

        SuspendInlineEditor();
        await _plannerStateStore.ToggleTaskCompletionAsync(
            _plannerStateStore.SelectedTask.Id,
            !_plannerStateStore.SelectedTask.IsCompleted);
        ResumeInlineEditor(focusTitleEditor: false);
    }

    private async void DeleteSelectedTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (_plannerStateStore.SelectedTask is null)
            return;

        SuspendInlineEditor();
        await _plannerStateStore.DeleteTaskAsync(_plannerStateStore.SelectedTask.Id);
        ResumeInlineEditor(focusTitleEditor: false);
    }

    private void CloseEditorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_timersInitialized)
            _inlineSaveTimer.Stop();

        SuspendInlineEditor();
        _plannerStateStore.SelectTask(null);
        ResumeInlineEditor(focusTitleEditor: false);
    }

    private void CurrentTimeTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        _plannerStateStore.RefreshCurrentTimeIndicator();
    }

    private void TodayPage_Unloaded(object sender, RoutedEventArgs e)
    {
        if (!_timersInitialized)
            return;

        _currentTimeTimer.Stop();
        _inlineSaveTimer.Stop();
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
        roundedMinutes = Math.Clamp(roundedMinutes, 0, (24 * 60) - TimelineSnapMinutes);

        return new TimeOnly(roundedMinutes / 60, roundedMinutes % 60);
    }

    private void ScheduleInlineSave()
    {
        if (!_timersInitialized || _suppressInlineSave)
            return;

        if (_isInlineSaveInProgress)
        {
            _inlineSaveQueued = true;
            return;
        }

        if (_inlineSaveTimer.IsRunning)
            _inlineSaveTimer.Stop();

        _inlineSaveTimer.Start();
    }

    private async Task SaveInlineEditorNowAsync()
    {
        if (_suppressInlineSave || _isInlineSaveInProgress)
            return;

        if (string.IsNullOrWhiteSpace(_plannerStateStore.EditorTitle))
            return;

        _isInlineSaveInProgress = true;
        _suppressInlineSave = true;

        try
        {
            await _plannerStateStore.SaveSelectedTaskAsync();
        }
        finally
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                _isInlineSaveInProgress = false;
                _suppressInlineSave = false;

                if (_inlineSaveQueued)
                {
                    _inlineSaveQueued = false;
                    ScheduleInlineSave();
                }
            });
        }
    }

    private void SuspendInlineEditor()
    {
        if (!_timersInitialized)
            return;

        _inlineSaveTimer.Stop();
        _suppressInlineSave = true;
    }

    private void ResumeInlineEditor(bool focusTitleEditor)
    {
        _focusTitleEditorAfterSelection = focusTitleEditor;
        DispatcherQueue.TryEnqueue(() =>
        {
            _suppressInlineSave = false;

            if (!_focusTitleEditorAfterSelection)
                return;

            _focusTitleEditorAfterSelection = false;
            EditorTitleTextBox.Focus(FocusState.Programmatic);
            EditorTitleTextBox.SelectAll();
        });
    }
}

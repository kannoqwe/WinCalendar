using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinCalendar.Modules.Planner.Presentation;
using WinCalendar.Shared.Windowing;

namespace WinCalendar.Modules.Planner.UI;

public sealed partial class TodayPage : Page
{
    private const int TimelineSnapMinutes = 15;
    private readonly PlannerStateStore _plannerStateStore;
    private readonly DispatcherQueueTimer _currentTimeTimer;
    private readonly DispatcherQueueTimer _inlineSaveTimer;
    private bool _syncingEditorTimeFlyout;
    private bool _timersInitialized;
    private bool _suppressInlineSave;
    private bool _isInlineSaveInProgress;
    private bool _inlineSaveQueued;
    private bool _focusTitleEditorAfterSelection;
    private bool _initialized;
    private bool _syncingWeekHorizontalScroll;
    private bool _syncingWeekVerticalScroll;

    public string[] EditorHourOptions { get; } = Enumerable.Range(0, 24)
        .Select(hour => hour.ToString("00"))
        .ToArray();

    public string[] EditorMinuteOptions { get; } = ["00", "15", "30", "45"];

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
            QueueCenterCurrentTimeIndicator();
            return;
        }

        _initialized = true;
        await _plannerStateStore.EnsureInitializedAsync();
        _plannerStateStore.RefreshCurrentTimeIndicator();
        StartCurrentTimeTimer();
        QueueCenterCurrentTimeIndicator();
    }

    private async void PreviousWeekButton_Click(object sender, RoutedEventArgs e)
    {
        await _plannerStateStore.GoToPreviousWeekAsync();
    }

    private async void CurrentWeekButton_Click(object sender, RoutedEventArgs e)
    {
        await _plannerStateStore.GoToTodayAsync();
        QueueCenterCurrentTimeIndicator();
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

        if (IsInsideButton(e.OriginalSource))
            return;

        e.Handled = true;
        SuspendInlineEditor();
        _plannerStateStore.BeginNewTaskDraft(day.Date);
        await _plannerStateStore.SelectDateAsync(day.Date);
        ResumeInlineEditor(focusTitleEditor: true);
    }

    private async void WeekTimedGrid_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PlannerWeekDayTimelineViewModel day } surface)
            return;

        double offsetY = GetTimeGridContentOffset(surface, e);
        TimeOnly time = GetTimeFromTimelinePosition(offsetY);

        SuspendInlineEditor();
        _plannerStateStore.BeginNewTaskDraft(day.Date, time);
        await _plannerStateStore.SelectDateAsync(day.Date);
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

    private void EditorTimeFlyout_Opening(object sender, object e)
    {
        _syncingEditorTimeFlyout = true;

        try
        {
            EditorHourComboBox.SelectedItem = _plannerStateStore.EditorTime.Hours.ToString("00");
            EditorMinuteComboBox.SelectedItem = _plannerStateStore.EditorTime.Minutes.ToString("00");
        }
        finally
        {
            _syncingEditorTimeFlyout = false;
        }
    }

    private void EditorTimePartComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingEditorTimeFlyout
            || EditorHourComboBox.SelectedItem is not string hourText
            || EditorMinuteComboBox.SelectedItem is not string minuteText
            || !int.TryParse(hourText, out int hour)
            || !int.TryParse(minuteText, out int minute))
        {
            return;
        }

        TimeSpan nextTime = new(hour, minute, 0);
        if (_plannerStateStore.EditorTime == nextTime)
            return;

        _plannerStateStore.EditorTime = nextTime;
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

    private void WeekTimelineStickyScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_syncingWeekHorizontalScroll || sender is not ScrollViewer source)
            return;

        SyncWeekHorizontalScroll(source.HorizontalOffset, source);
    }

    private void WeekTimelineHoursScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_syncingWeekVerticalScroll || sender is not ScrollViewer source)
            return;

        SyncWeekVerticalScroll(source.VerticalOffset, source);
    }

    private void WeekTimelineScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (!_syncingWeekHorizontalScroll)
            SyncWeekHorizontalScroll(WeekTimelineScrollViewer.HorizontalOffset, WeekTimelineScrollViewer);

        if (!_syncingWeekVerticalScroll)
            SyncWeekVerticalScroll(WeekTimelineScrollViewer.VerticalOffset, WeekTimelineScrollViewer);
    }

    private void QueueCenterCurrentTimeIndicator()
    {
        if (_plannerStateStore.CurrentWeekTimeIndicatorVisibility != Visibility.Visible)
            return;

        DispatcherQueue.TryEnqueue(() => CenterCurrentTimeIndicatorInView(6));
    }

    private void CenterCurrentTimeIndicatorInView(int attemptsRemaining)
    {
        if (_plannerStateStore.CurrentWeekTimeIndicatorVisibility != Visibility.Visible)
            return;

        double viewportHeight = WeekTimelineScrollViewer.ViewportHeight;
        if (viewportHeight <= 0d)
        {
            if (attemptsRemaining <= 0)
                return;

            DispatcherQueue.TryEnqueue(() => CenterCurrentTimeIndicatorInView(attemptsRemaining - 1));
            return;
        }

        double targetOffset = _plannerStateStore.CurrentTimeIndicatorTop - (viewportHeight / 2d);
        double clampedOffset = Math.Clamp(targetOffset, 0d, Math.Max(0d, WeekTimelineScrollViewer.ScrollableHeight));
        WeekTimelineScrollViewer.ChangeView(null, clampedOffset, null, true);
    }

    private void SyncWeekHorizontalScroll(double horizontalOffset, ScrollViewer source)
    {
        _syncingWeekHorizontalScroll = true;

        try
        {
            if (!ReferenceEquals(source, WeekTimelineHeaderScrollViewer))
                WeekTimelineHeaderScrollViewer.ChangeView(horizontalOffset, null, null, true);

            if (!ReferenceEquals(source, WeekTimelineAllDayScrollViewer))
                WeekTimelineAllDayScrollViewer.ChangeView(horizontalOffset, null, null, true);

            if (!ReferenceEquals(source, WeekTimelineScrollViewer))
                WeekTimelineScrollViewer.ChangeView(horizontalOffset, null, null, true);
        }
        finally
        {
            _syncingWeekHorizontalScroll = false;
        }
    }

    private void SyncWeekVerticalScroll(double verticalOffset, ScrollViewer source)
    {
        _syncingWeekVerticalScroll = true;

        try
        {
            if (!ReferenceEquals(source, WeekTimelineHoursScrollViewer))
                WeekTimelineHoursScrollViewer.ChangeView(null, verticalOffset, null, true);

            if (!ReferenceEquals(source, WeekTimelineScrollViewer))
                WeekTimelineScrollViewer.ChangeView(null, verticalOffset, null, true);
        }
        finally
        {
            _syncingWeekVerticalScroll = false;
        }
    }

    private TimeOnly GetTimeFromTimelinePosition(double offsetY)
    {
        double rawMinutes = Math.Max(0d, (offsetY / _plannerStateStore.WeekTimelineHourHeight) * 60d);
        int roundedMinutes = (int)(Math.Round(rawMinutes / TimelineSnapMinutes) * TimelineSnapMinutes);
        roundedMinutes = Math.Clamp(roundedMinutes, 0, (24 * 60) - TimelineSnapMinutes);

        return new TimeOnly(roundedMinutes / 60, roundedMinutes % 60);
    }

    private double GetTimeGridContentOffset(FrameworkElement surface, TappedRoutedEventArgs e)
    {
        double surfaceOffsetY = e.GetPosition(surface).Y;
        return Math.Clamp(surfaceOffsetY, 0d, _plannerStateStore.WeekTimelineTimedHeight);
    }

    private static bool IsInsideButton(object? originalSource)
    {
        DependencyObject? current = originalSource as DependencyObject;

        while (current is not null)
        {
            if (current is Button)
                return true;

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
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

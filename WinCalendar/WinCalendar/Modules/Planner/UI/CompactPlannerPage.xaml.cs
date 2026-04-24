using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using WinCalendar.Modules.Planner.Presentation;

namespace WinCalendar.Modules.Planner.UI;

public sealed partial class CompactPlannerPage : Page
{
    private readonly PlannerStateStore _plannerStateStore;
    private readonly Action _openMainApp;
    private bool _syncingCompactEditorDateSelection;
    private bool _syncingCompactEditorTimeSelection;
    private bool _initialized;

    public string[] CompactEditorHourOptions { get; } = Enumerable.Range(0, 24)
        .Select(hour => hour.ToString("00"))
        .ToArray();

    public string[] CompactEditorMinuteOptions { get; } = ["00", "15", "30", "45"];

    public CompactPlannerPage(PlannerStateStore plannerStateStore, Action openMainApp)
    {
        _plannerStateStore = plannerStateStore;
        _openMainApp = openMainApp;
        InitializeComponent();
        DataContext = _plannerStateStore;
    }

    private async void Root_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
            return;

        _initialized = true;
        await _plannerStateStore.EnsureInitializedAsync();

        if (_plannerStateStore.SelectedDate != _plannerStateStore.Today)
            await _plannerStateStore.GoToTodayAsync();
    }

    private void AddTaskButton_Click(object sender, RoutedEventArgs e)
    {
        _plannerStateStore.BeginNewTaskDraft(_plannerStateStore.SelectedDate);
        UpdateCompactEditorDoneButtonState();

        DispatcherQueue.TryEnqueue(() =>
        {
            AnimateCompactEditorOpen();
            CompactEditorTitleTextBox.Focus(FocusState.Programmatic);
            CompactEditorTitleTextBox.SelectAll();
        });
    }

    private async void MonthDayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlannerMonthDayViewModel day })
            return;

        await _plannerStateStore.SelectDateAsync(day.Date);
    }

    private async void TaskCompletionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlannerTaskViewModel task } button)
            return;

        button.IsEnabled = false;

        try
        {
            await _plannerStateStore.ToggleTaskCompletionAsync(task.Id, !task.IsCompleted);
        }
        finally
        {
            button.IsEnabled = true;
        }
    }

    private async void PreviousMonthButton_Click(object sender, RoutedEventArgs e)
    {
        await _plannerStateStore.BrowsePreviousMonthAsync();
    }

    private async void NextMonthButton_Click(object sender, RoutedEventArgs e)
    {
        await _plannerStateStore.BrowseNextMonthAsync();
    }

    private void OpenMainAppButton_Click(object sender, RoutedEventArgs e)
    {
        _openMainApp();
    }

    private void CompactEditorTitleTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateCompactEditorDoneButtonState();
    }

    private void CompactEditorDateFlyout_Opening(object sender, object e)
    {
        _syncingCompactEditorDateSelection = true;

        try
        {
            CompactEditorDateCalendarView.SelectedDates.Clear();
            CompactEditorDateCalendarView.SelectedDates.Add(_plannerStateStore.EditorDate);
        }
        finally
        {
            _syncingCompactEditorDateSelection = false;
        }
    }

    private void CompactEditorDateCalendarView_SelectedDatesChanged(
        CalendarView sender,
        CalendarViewSelectedDatesChangedEventArgs args)
    {
        if (_syncingCompactEditorDateSelection || sender.SelectedDates.Count == 0)
            return;

        _plannerStateStore.EditorDate = sender.SelectedDates[0];
        CompactEditorDateFlyout.Hide();
    }

    private void CompactEditorTimeFlyout_Opening(object sender, object e)
    {
        _syncingCompactEditorTimeSelection = true;

        try
        {
            CompactEditorHourListView.SelectedItem = _plannerStateStore.EditorTime.Hours.ToString("00");
            CompactEditorMinuteListView.SelectedItem = _plannerStateStore.EditorTime.Minutes.ToString("00");
        }
        finally
        {
            _syncingCompactEditorTimeSelection = false;
        }
    }

    private void CompactEditorTimeListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingCompactEditorTimeSelection
            || CompactEditorHourListView.SelectedItem is not string hourText
            || CompactEditorMinuteListView.SelectedItem is not string minuteText
            || !int.TryParse(hourText, out int hour)
            || !int.TryParse(minuteText, out int minute))
        {
            return;
        }

        _plannerStateStore.EditorTime = new TimeSpan(hour, minute, 0);
    }

    private void CompactEditorDurationFlyout_Opening(object sender, object e)
    {
        PlannerEditorDurationOptionViewModel? selectedOption = _plannerStateStore.EditorDurationOptions
            .FirstOrDefault(option => option.DurationMinutes == (int)Math.Round(_plannerStateStore.EditorDurationMinutes));

        CompactEditorDurationListView.SelectedItem = selectedOption;
    }

    private void CompactEditorDurationListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not PlannerEditorDurationOptionViewModel option)
            return;

        _plannerStateStore.EditorDurationMinutes = option.DurationMinutes;
        CompactEditorDurationFlyout.Hide();
    }

    private void CancelCompactEditorButton_Click(object sender, RoutedEventArgs e)
    {
        _plannerStateStore.SelectTask(null);
        UpdateCompactEditorDoneButtonState();
    }

    private async void DoneCompactEditorButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_plannerStateStore.EditorTitle))
            return;

        CompactEditorDoneButton.IsEnabled = false;

        try
        {
            await _plannerStateStore.SaveSelectedTaskAsync();
            _plannerStateStore.SelectTask(null);
        }
        finally
        {
            UpdateCompactEditorDoneButtonState();
        }
    }

    private void UpdateCompactEditorDoneButtonState()
    {
        if (CompactEditorDoneButton is null)
            return;

        CompactEditorDoneButton.IsEnabled = !string.IsNullOrWhiteSpace(_plannerStateStore.EditorTitle);
    }

    private void AnimateCompactEditorOpen()
    {
        CompactEditorHost.Opacity = 0;
        CompactEditorHostTranslate.Y = 12;

        Storyboard storyboard = new();
        DoubleAnimation opacityAnimation = new()
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(180),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        DoubleAnimation translateAnimation = new()
        {
            From = 12,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(180),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        Storyboard.SetTarget(opacityAnimation, CompactEditorHost);
        Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
        Storyboard.SetTarget(translateAnimation, CompactEditorHostTranslate);
        Storyboard.SetTargetProperty(translateAnimation, "Y");
        storyboard.Children.Add(opacityAnimation);
        storyboard.Children.Add(translateAnimation);
        storyboard.Begin();
    }
}

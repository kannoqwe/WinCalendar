using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using WinCalendar.Modules.Planner.Presentation;
using WinCalendar.Shared.Windowing;

namespace WinCalendar.Modules.Planner.UI;

public sealed partial class MediumPlannerPage : Page
{
    private readonly PlannerStateStore _plannerStateStore;
    private readonly PlannerWindowCoordinator _windowCoordinator;
    private bool _initialized;

    public MediumPlannerPage(PlannerStateStore plannerStateStore, PlannerWindowCoordinator windowCoordinator)
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

    private async void PreviousDayButton_Click(object sender, RoutedEventArgs e)
    {
        await _plannerStateStore.GoToPreviousDayAsync();
    }

    private async void NextDayButton_Click(object sender, RoutedEventArgs e)
    {
        await _plannerStateStore.GoToNextDayAsync();
    }

    private async void QuickAddButton_Click(object sender, RoutedEventArgs e)
    {
        await AddSelectedDayTaskAsync();
    }

    private async void QuickAddTitleTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter)
            return;

        e.Handled = true;
        await AddSelectedDayTaskAsync();
    }

    private async void TaskCompletionCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox { DataContext: PlannerTaskViewModel task } checkBox)
            return;

        await _plannerStateStore.ToggleTaskCompletionAsync(task.Id, checkBox.IsChecked == true);
    }

    private void TaskCardButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlannerTaskViewModel task })
            return;

        _plannerStateStore.SelectTask(task);
    }

    private async void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlannerTaskViewModel task })
            return;

        await _plannerStateStore.DeleteTaskAsync(task.Id);
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

    private void OpenFullButton_Click(object sender, RoutedEventArgs e)
    {
        _windowCoordinator.ShowFullApp();
    }

    private async Task AddSelectedDayTaskAsync()
    {
        string title = QuickAddTitleTextBox.Text;
        TimeOnly? time = QuickAddUseTimeToggle.IsOn
            ? TimeOnly.FromTimeSpan(QuickAddTimePicker.Time)
            : null;

        await _plannerStateStore.AddTaskAsync(title, _plannerStateStore.SelectedDate, time);
        QuickAddTitleTextBox.Text = string.Empty;
        QuickAddUseTimeToggle.IsOn = false;
        QuickAddTimePicker.Time = new TimeSpan(9, 0, 0);
    }
}

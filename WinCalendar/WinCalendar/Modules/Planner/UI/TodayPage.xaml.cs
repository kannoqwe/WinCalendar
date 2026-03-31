using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
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

    private async void QuickAddButton_Click(object sender, RoutedEventArgs e)
    {
        await AddTodayTaskAsync();
    }

    private async void QuickAddTitleTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter)
            return;

        e.Handled = true;
        await AddTodayTaskAsync();
    }

    private async void TodayTaskCompletionCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox { DataContext: PlannerTaskViewModel task } checkBox)
            return;

        await _plannerStateStore.ToggleTaskCompletionAsync(task.Id, checkBox.IsChecked == true);
    }

    private async void TodayTaskOpenButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlannerTaskViewModel task })
            return;

        await _plannerStateStore.SelectDateAsync(task.Date);
        _plannerStateStore.SelectTask(task);
        _windowCoordinator.ShowMediumView();
    }

    private void OpenCompactButton_Click(object sender, RoutedEventArgs e)
    {
        _windowCoordinator.ShowCompactPanel();
    }

    private void OpenMediumButton_Click(object sender, RoutedEventArgs e)
    {
        _windowCoordinator.ShowMediumView();
    }

    private async Task AddTodayTaskAsync()
    {
        TimeOnly? time = QuickAddUseTimeToggle.IsOn
            ? TimeOnly.FromTimeSpan(QuickAddTimePicker.Time)
            : null;

        await _plannerStateStore.AddTaskAsync(
            QuickAddTitleTextBox.Text,
            DateOnly.FromDateTime(DateTime.Today),
            time);

        QuickAddTitleTextBox.Text = string.Empty;
        QuickAddUseTimeToggle.IsOn = false;
        QuickAddTimePicker.Time = new TimeSpan(9, 0, 0);
    }
}

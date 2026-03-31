using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCalendar.Modules.Planner.Presentation;
using WinCalendar.Shared.Windowing;

namespace WinCalendar.Modules.Planner.UI;

public sealed partial class CompactPanelPage : Page
{
    private readonly PlannerStateStore _plannerStateStore;
    private readonly PlannerWindowCoordinator _windowCoordinator;
    private bool _initialized;

    public CompactPanelPage(PlannerStateStore plannerStateStore, PlannerWindowCoordinator windowCoordinator)
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

    private void ToggleSidebarButton_Click(object sender, RoutedEventArgs e)
    {
        _plannerStateStore.ToggleCompactSidebar();
    }

    private async void PreviousMonthButton_Click(object sender, RoutedEventArgs e)
    {
        await _plannerStateStore.GoToPreviousMonthAsync();
    }

    private async void NextMonthButton_Click(object sender, RoutedEventArgs e)
    {
        await _plannerStateStore.GoToNextMonthAsync();
    }

    private async void TodayButton_Click(object sender, RoutedEventArgs e)
    {
        await _plannerStateStore.GoToTodayAsync();
    }

    private async void MonthDayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlannerMonthDayViewModel day })
            return;

        await _plannerStateStore.SelectDateAsync(day.Date);
    }

    private async void AgendaDayHeaderButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlannerAgendaDayViewModel day })
            return;

        await _plannerStateStore.SelectDateAsync(day.Date);
    }

    private void AgendaDayExpandButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlannerAgendaDayViewModel day })
            return;

        _plannerStateStore.ToggleAgendaDayExpanded(day);
    }

    private void AgendaDayHideButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlannerAgendaDayViewModel day })
            return;

        _plannerStateStore.ToggleAgendaDayHidden(day);
    }

    private async void CompactTaskCompletionCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox { DataContext: PlannerTaskViewModel task } checkBox)
            return;

        await _plannerStateStore.ToggleTaskCompletionAsync(task.Id, checkBox.IsChecked == true);
    }

    private void CompactOpenTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlannerTaskViewModel task })
            return;

        _plannerStateStore.SelectTask(task);
        _windowCoordinator.ShowMediumView();
    }

    private void OpenMediumButton_Click(object sender, RoutedEventArgs e)
    {
        _windowCoordinator.ShowMediumView();
    }

    private void OpenFullButton_Click(object sender, RoutedEventArgs e)
    {
        _windowCoordinator.ShowFullApp();
    }
}

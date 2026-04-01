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
        _windowCoordinator.ToggleCompactSidebar();
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

    private void OpenMediumButton_Click(object sender, RoutedEventArgs e)
    {
        _windowCoordinator.ShowMediumView();
    }

    private void OpenFullButton_Click(object sender, RoutedEventArgs e)
    {
        _windowCoordinator.ShowFullApp();
    }
}

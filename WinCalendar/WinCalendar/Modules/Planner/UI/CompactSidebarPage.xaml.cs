using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCalendar.Modules.Planner.Presentation;
using WinCalendar.Shared.Windowing;

namespace WinCalendar.Modules.Planner.UI;

public sealed partial class CompactSidebarPage : Page
{
    private readonly PlannerStateStore _plannerStateStore;
    private readonly PlannerWindowCoordinator _windowCoordinator;
    private bool _initialized;

    public CompactSidebarPage(PlannerStateStore plannerStateStore, PlannerWindowCoordinator windowCoordinator)
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
}

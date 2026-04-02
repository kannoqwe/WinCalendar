using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCalendar.Modules.Planner.Presentation;
using WinCalendar.Shared.Windowing;

namespace WinCalendar.Modules.Planner.UI;

public sealed partial class CompactSidebarPage : Page
{
    private readonly PlannerStateStore _plannerStateStore;
    private bool _initialized;

    public CompactSidebarPage(PlannerStateStore plannerStateStore, PlannerWindowCoordinator windowCoordinator)
    {
        _plannerStateStore = plannerStateStore;
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

    private async void AgendaDayHeaderButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlannerAgendaDayViewModel day })
            return;

        _plannerStateStore.ToggleAgendaDayExpanded(day);
        await _plannerStateStore.SelectDateAsync(day.Date);
    }

    private async void AgendaTaskCompletionCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox { DataContext: PlannerTaskViewModel task } checkBox)
            return;

        await _plannerStateStore.ToggleTaskCompletionAsync(task.Id, checkBox.IsChecked == true);
    }
}

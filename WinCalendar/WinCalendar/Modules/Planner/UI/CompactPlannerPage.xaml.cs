using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCalendar.Modules.Planner.Presentation;

namespace WinCalendar.Modules.Planner.UI;

public sealed partial class CompactPlannerPage : Page
{
    private readonly PlannerStateStore _plannerStateStore;
    private bool _initialized;

    public CompactPlannerPage(PlannerStateStore plannerStateStore)
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

        if (_plannerStateStore.SelectedDate != _plannerStateStore.Today)
            await _plannerStateStore.GoToTodayAsync();
    }

    private async void AddTaskButton_Click(object sender, RoutedEventArgs e)
    {
        await TaskComposerDialogService.ShowAddTaskAsync(XamlRoot, _plannerStateStore, _plannerStateStore.SelectedDate);
    }

    private async void MonthDayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlannerMonthDayViewModel day })
            return;

        await _plannerStateStore.SelectDateAsync(day.Date);
    }

    private async void PreviousMonthButton_Click(object sender, RoutedEventArgs e)
    {
        await _plannerStateStore.BrowsePreviousMonthAsync();
    }

    private async void NextMonthButton_Click(object sender, RoutedEventArgs e)
    {
        await _plannerStateStore.BrowseNextMonthAsync();
    }
}

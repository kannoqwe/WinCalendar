using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCalendar.Modules.Planner.Presentation;
using WinCalendar.Modules.Planner.UI;

namespace WinCalendar.Modules.Calendar.UI;

public sealed partial class CalendarPage : Page
{
    private readonly PlannerStateStore _plannerStateStore;
    private bool _initialized;

    public CalendarPage(PlannerStateStore plannerStateStore)
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

    private async void PreviousMonthButton_Click(object sender, RoutedEventArgs e)
    {
        await _plannerStateStore.GoToPreviousMonthAsync();
    }

    private async void NextMonthButton_Click(object sender, RoutedEventArgs e)
    {
        await _plannerStateStore.GoToNextMonthAsync();
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

    private async void AddTaskButton_Click(object sender, RoutedEventArgs e)
    {
        await TaskComposerDialogService.ShowAddTaskAsync(XamlRoot, _plannerStateStore, _plannerStateStore.SelectedDate);
    }
}

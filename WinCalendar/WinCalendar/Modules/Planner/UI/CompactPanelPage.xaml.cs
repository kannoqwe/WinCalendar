using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinCalendar.Modules.Planner.Presentation;

namespace WinCalendar.Modules.Planner.UI;

public sealed partial class CompactPanelPage : Page
{
    private readonly PlannerStateStore _plannerStateStore;
    private bool _initialized;

    public CompactPanelPage(PlannerStateStore plannerStateStore)
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

    private async void MonthDayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlannerMonthDayViewModel day })
            return;

        await _plannerStateStore.SelectDateAsync(day.Date);
    }

    private async void MonthDayButton_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlannerMonthDayViewModel day })
            return;

        e.Handled = true;
        await TaskComposerDialogService.ShowAddTaskAsync(XamlRoot, _plannerStateStore, day.Date);
    }

    private async void AddTaskButton_Click(object sender, RoutedEventArgs e)
    {
        await TaskComposerDialogService.ShowAddTaskAsync(XamlRoot, _plannerStateStore, _plannerStateStore.Today);
    }

    private async void CompactTaskCompletionCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox { DataContext: PlannerTaskViewModel task } checkBox)
            return;

        checkBox.IsEnabled = false;

        try
        {
            await _plannerStateStore.ToggleTaskCompletionAsync(task.Id, checkBox.IsChecked == true);
        }
        finally
        {
            checkBox.IsEnabled = true;
        }
    }

    private void PreviousTaskPageButton_Click(object sender, RoutedEventArgs e)
    {
        _plannerStateStore.GoToPreviousCompactTaskPage();
    }

    private void NextTaskPageButton_Click(object sender, RoutedEventArgs e)
    {
        _plannerStateStore.GoToNextCompactTaskPage();
    }
}

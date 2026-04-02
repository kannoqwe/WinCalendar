using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinCalendar.Core.Time;
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

    private async void MonthDayButton_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlannerMonthDayViewModel day })
            return;

        e.Handled = true;
        await ShowAddTaskDialogAsync(day.Date);
    }

    private void OpenMediumButton_Click(object sender, RoutedEventArgs e)
    {
        _windowCoordinator.ShowMediumView();
    }

    private void OpenFullButton_Click(object sender, RoutedEventArgs e)
    {
        _windowCoordinator.ShowFullApp();
    }

    private async Task ShowAddTaskDialogAsync(DateOnly date)
    {
        TextBox titleTextBox = new()
        {
            Header = "Task",
            PlaceholderText = "What needs to be done?"
        };

        ToggleSwitch useTimeToggle = new()
        {
            Header = "Use time"
        };

        TimePicker timePicker = new()
        {
            Time = new TimeSpan(9, 0, 0),
            IsEnabled = false
        };

        StackPanel content = new()
        {
            Spacing = 12
        };
        content.Children.Add(titleTextBox);
        content.Children.Add(useTimeToggle);
        content.Children.Add(timePicker);

        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = $"Add task for {PlannerDateTimeFormatter.FormatDate(date)}",
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
            Content = content
        };

        useTimeToggle.Toggled += (_, _) => timePicker.IsEnabled = useTimeToggle.IsOn;
        titleTextBox.TextChanged += (_, _) => dialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(titleTextBox.Text);
        dialog.Opened += (_, _) => titleTextBox.Focus(FocusState.Programmatic);

        ContentDialogResult result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return;

        TimeOnly? time = useTimeToggle.IsOn
            ? TimeOnly.FromTimeSpan(timePicker.Time)
            : null;

        await _plannerStateStore.AddTaskAsync(titleTextBox.Text, date, time);
    }
}

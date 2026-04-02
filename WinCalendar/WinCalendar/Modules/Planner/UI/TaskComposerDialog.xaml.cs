using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCalendar.Core.Time;

namespace WinCalendar.Modules.Planner.UI;

public sealed partial class TaskComposerDialog : ContentDialog
{
    public TaskComposerDialog(XamlRoot xamlRoot, DateOnly initialDate)
    {
        InitializeComponent();
        XamlRoot = xamlRoot;
        TaskDatePicker.Date = new DateTimeOffset(initialDate.ToDateTime(TimeOnly.MinValue));
        TaskTimePicker.Time = new TimeSpan(9, 0, 0);
        IsPrimaryButtonEnabled = false;
        Opened += TaskComposerDialog_Opened;
        UpdateSubtitle();
    }

    public async Task<TaskComposerResult?> ShowForResultAsync()
    {
        ContentDialogResult result = await ShowAsync();
        if (result != ContentDialogResult.Primary)
            return null;

        return new TaskComposerResult(
            TitleTextBox.Text.Trim(),
            DateOnly.FromDateTime(TaskDatePicker.Date.Date),
            HasTimeToggle.IsOn
                ? TimeOnly.FromTimeSpan(TaskTimePicker.Time)
                : null);
    }

    private void TaskComposerDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        TitleTextBox.Focus(FocusState.Programmatic);
    }

    private void TitleTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(TitleTextBox.Text);
    }

    private void TaskDatePicker_DateChanged(object sender, DatePickerValueChangedEventArgs args)
    {
        UpdateSubtitle();
    }

    private void HasTimeToggle_Toggled(object sender, RoutedEventArgs e)
    {
        TaskTimePicker.IsEnabled = HasTimeToggle.IsOn;
        UpdateSubtitle();
    }

    private void UpdateSubtitle()
    {
        DateOnly selectedDate = DateOnly.FromDateTime(TaskDatePicker.Date.Date);
        string timeMode = HasTimeToggle.IsOn ? "Time enabled" : "Any time";
        SubtitleTextBlock.Text = $"{PlannerDateTimeFormatter.FormatDate(selectedDate)} | {timeMode}";
    }
}

public sealed record TaskComposerResult(string Title, DateOnly Date, TimeOnly? Time);

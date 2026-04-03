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
        TaskDurationNumberBox.Value = 45;
        IsPrimaryButtonEnabled = false;
        Opened += TaskComposerDialog_Opened;
        UpdateDurationControls();
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
                : null,
            HasTimeToggle.IsOn && HasDurationToggle.IsOn
                ? (int)Math.Round(TaskDurationNumberBox.Value)
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
        HasDurationToggle.IsEnabled = HasTimeToggle.IsOn;

        if (!HasTimeToggle.IsOn)
            HasDurationToggle.IsOn = false;

        UpdateDurationControls();
        UpdateSubtitle();
    }

    private void TaskTimePicker_TimeChanged(object sender, TimePickerValueChangedEventArgs args)
    {
        UpdateDurationControls();
        UpdateSubtitle();
    }

    private void HasDurationToggle_Toggled(object sender, RoutedEventArgs e)
    {
        UpdateDurationControls();
        UpdateSubtitle();
    }

    private void TaskDurationNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        UpdateDurationControls();
        UpdateSubtitle();
    }

    private void UpdateSubtitle()
    {
        DateOnly selectedDate = DateOnly.FromDateTime(TaskDatePicker.Date.Date);
        string scheduleText = HasTimeToggle.IsOn
            ? PlannerDateTimeFormatter.FormatTime(TimeOnly.FromTimeSpan(TaskTimePicker.Time))
            : "Any time";
        string durationText = HasTimeToggle.IsOn && HasDurationToggle.IsOn
            ? PlannerDateTimeFormatter.FormatDuration((int)Math.Round(TaskDurationNumberBox.Value))
            : "No duration";

        SubtitleTextBlock.Text = $"{PlannerDateTimeFormatter.FormatDate(selectedDate)} | {scheduleText} | {durationText}";
    }

    private void UpdateDurationControls()
    {
        HasDurationToggle.IsEnabled = HasTimeToggle.IsOn;
        TaskDurationNumberBox.IsEnabled = HasTimeToggle.IsOn && HasDurationToggle.IsOn;
        TaskDurationNumberBox.Maximum = GetMaxDurationMinutes();
        TaskDurationNumberBox.Value = NormalizeDurationValue(TaskDurationNumberBox.Value, TaskDurationNumberBox.Maximum);
    }

    private double GetMaxDurationMinutes()
    {
        if (!HasTimeToggle.IsOn)
            return 24 * 60;

        return (24 * 60) - TaskTimePicker.Time.TotalMinutes;
    }

    private static double NormalizeDurationValue(double value, double maxDurationMinutes)
    {
        double normalizedValue = double.IsNaN(value)
            ? 45
            : Math.Round(value / 5d) * 5d;

        return Math.Clamp(normalizedValue, 5d, maxDurationMinutes);
    }
}

public sealed record TaskComposerResult(string Title, DateOnly Date, TimeOnly? Time, int? DurationMinutes);

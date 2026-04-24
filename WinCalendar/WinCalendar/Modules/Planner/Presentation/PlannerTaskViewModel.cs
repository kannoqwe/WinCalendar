using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Planner.App.Modules.Tasks.Entities;
using WinCalendar.Core.Time;

namespace WinCalendar.Modules.Planner.Presentation;

public sealed class PlannerTaskViewModel
{
    public PlannerTaskViewModel(TaskItem task)
    {
        PlannerTaskPalette.PlannerTaskTone tone = PlannerTaskPalette.GetTone(task.Category);

        Id = task.Id;
        Title = task.Title;
        Description = task.Description;
        Date = task.Date;
        Time = task.Time;
        DurationMinutes = task.DurationMinutes;
        RecurrencePattern = task.RecurrencePattern;
        Category = task.Category;
        IsCompleted = task.IsCompleted;
        TaskBackgroundBrush = tone.BackgroundBrush;
        TaskBorderBrush = tone.BorderBrush;
        TaskAccentBrush = tone.AccentBrush;
    }

    public Guid Id { get; }

    public string Title { get; }

    public string Description { get; }

    public DateOnly Date { get; }

    public TimeOnly? Time { get; }

    public int? DurationMinutes { get; }

    public TaskRecurrencePattern RecurrencePattern { get; }

    public TaskCategory Category { get; }

    public bool IsCompleted { get; }

    public SolidColorBrush TaskBackgroundBrush { get; }

    public SolidColorBrush TaskBorderBrush { get; }

    public SolidColorBrush TaskAccentBrush { get; }

    public string DateText => PlannerDateTimeFormatter.FormatDate(Date);

    public string TimeText => PlannerDateTimeFormatter.FormatTime(Time);

    public string TimeRangeText => PlannerDateTimeFormatter.FormatTimeRange(Time, DurationMinutes);

    public string DetailsText => PlannerDateTimeFormatter.FormatDateTime(Date, Time, DurationMinutes);

    public string CompactTimeText => PlannerDateTimeFormatter.FormatCompactTime(Time, DurationMinutes);

    public string DurationText => PlannerDateTimeFormatter.FormatDuration(DurationMinutes);

    public string RecurrenceText => RecurrencePattern switch
    {
        TaskRecurrencePattern.Daily => "Daily",
        TaskRecurrencePattern.Weekly => "Weekly",
        TaskRecurrencePattern.Monthly => "Monthly",
        _ => "Does not repeat"
    };

    public string CategoryText => Category switch
    {
        TaskCategory.Personal => "Personal",
        TaskCategory.Health => "Health",
        TaskCategory.Important => "Important",
        _ => "Work"
    };

    public bool HasTime => Time is not null;

    public bool HasDuration => DurationMinutes is > 0;

    public Visibility CompletedVisibility => IsCompleted ? Visibility.Visible : Visibility.Collapsed;

    public double ContentOpacity => IsCompleted ? 0.58 : 1.0;
}

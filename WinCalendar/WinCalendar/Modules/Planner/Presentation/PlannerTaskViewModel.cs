using System;
using Microsoft.UI.Xaml;
using Planner.App.Modules.Tasks.Entities;
using WinCalendar.Core.Time;

namespace WinCalendar.Modules.Planner.Presentation;

public sealed class PlannerTaskViewModel
{
    public PlannerTaskViewModel(TaskItem task)
    {
        Id = task.Id;
        Title = task.Title;
        Date = task.Date;
        Time = task.Time;
        DurationMinutes = task.DurationMinutes;
        IsCompleted = task.IsCompleted;
    }

    public Guid Id { get; }

    public string Title { get; }

    public DateOnly Date { get; }

    public TimeOnly? Time { get; }

    public int? DurationMinutes { get; }

    public bool IsCompleted { get; }

    public string DateText => PlannerDateTimeFormatter.FormatDate(Date);

    public string TimeText => PlannerDateTimeFormatter.FormatTime(Time);

    public string TimeRangeText => PlannerDateTimeFormatter.FormatTimeRange(Time, DurationMinutes);

    public string DetailsText => PlannerDateTimeFormatter.FormatDateTime(Date, Time, DurationMinutes);

    public string CompactTimeText => PlannerDateTimeFormatter.FormatCompactTime(Time, DurationMinutes);

    public string DurationText => PlannerDateTimeFormatter.FormatDuration(DurationMinutes);

    public bool HasTime => Time is not null;

    public bool HasDuration => DurationMinutes is > 0;

    public Visibility CompletedVisibility => IsCompleted ? Visibility.Visible : Visibility.Collapsed;

    public double ContentOpacity => IsCompleted ? 0.58 : 1.0;
}

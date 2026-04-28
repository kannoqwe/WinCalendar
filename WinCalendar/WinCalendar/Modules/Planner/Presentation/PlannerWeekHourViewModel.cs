using System;
using WinCalendar.Core.Time;

namespace WinCalendar.Modules.Planner.Presentation;

public sealed class PlannerWeekHourViewModel
{
    public PlannerWeekHourViewModel(int hour)
    {
        Hour = hour;
    }

    public int Hour { get; }

    public string LabelText => PlannerDateTimeFormatter.FormatTime(new TimeOnly(Hour, 0));
}

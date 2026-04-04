using System;
using Microsoft.UI.Xaml;

namespace WinCalendar.Modules.Planner.Presentation;

public sealed class PlannerWeekTimeSlotViewModel
{
    public PlannerWeekTimeSlotViewModel(DateOnly date, TimeOnly time)
    {
        Date = date;
        Time = time;
    }

    public DateOnly Date { get; }

    public TimeOnly Time { get; }

    public bool IsHourBoundary => Time.Minute == 0;

    public Thickness SeparatorThickness => new(0, IsHourBoundary ? 1 : 0.5, 0, 0);

    public double SeparatorOpacity => IsHourBoundary ? 1d : 0.52d;
}

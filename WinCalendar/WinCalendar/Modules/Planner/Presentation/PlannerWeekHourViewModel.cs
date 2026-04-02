namespace WinCalendar.Modules.Planner.Presentation;

public sealed class PlannerWeekHourViewModel
{
    public PlannerWeekHourViewModel(int hour)
    {
        Hour = hour;
    }

    public int Hour { get; }

    public string LabelText => $"{Hour:00}:00";
}

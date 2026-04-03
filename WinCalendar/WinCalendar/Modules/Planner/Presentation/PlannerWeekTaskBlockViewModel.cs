namespace WinCalendar.Modules.Planner.Presentation;

public sealed class PlannerWeekTaskBlockViewModel
{
    public PlannerWeekTaskBlockViewModel(
        PlannerTaskViewModel task,
        double top,
        double left,
        double width,
        double height)
    {
        Task = task;
        Top = top;
        Left = left;
        Width = width;
        Height = height;
    }

    public PlannerTaskViewModel Task { get; }

    public double Top { get; }

    public double Left { get; }

    public double Width { get; }

    public double Height { get; }

    public string Title => Task.Title;

    public string TimeText => Task.TimeRangeText;

    public double ContentOpacity => Task.ContentOpacity;
}

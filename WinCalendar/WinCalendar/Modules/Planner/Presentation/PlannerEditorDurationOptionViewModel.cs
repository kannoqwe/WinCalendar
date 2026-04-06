namespace WinCalendar.Modules.Planner.Presentation;

public sealed class PlannerEditorDurationOptionViewModel
{
    public PlannerEditorDurationOptionViewModel(int durationMinutes, string endTimeText, string descriptionText)
    {
        DurationMinutes = durationMinutes;
        EndTimeText = endTimeText;
        DescriptionText = descriptionText;
    }

    public int DurationMinutes { get; }

    public string EndTimeText { get; }

    public string DescriptionText { get; }
}

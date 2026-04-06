namespace WinCalendar.Modules.Planner.Presentation;

public sealed class PlannerEditorOptionViewModel
{
    public PlannerEditorOptionViewModel(int value, string label)
    {
        Value = value;
        Label = label;
    }

    public int Value { get; }

    public string Label { get; }
}

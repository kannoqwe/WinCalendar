using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;

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

    public SolidColorBrush BackgroundBrush => Task.TaskBackgroundBrush;

    public SolidColorBrush BorderBrush => Task.TaskBorderBrush;

    public SolidColorBrush AccentBrush => Task.TaskAccentBrush;

    public Thickness ContentPadding =>
        Height >= 72
            ? new Thickness(6, 5, 6, 5)
            : Height >= 44
                ? new Thickness(5, 4, 5, 4)
                : new Thickness(4, 3, 4, 3);

    public int TitleMaxLines =>
        Height >= 124
            ? 4
            : Height >= 88
                ? 3
                : Height >= 52
                    ? 2
                    : 1;

    public TextWrapping TitleWrapping =>
        TitleMaxLines > 1 ? TextWrapping.WrapWholeWords : TextWrapping.NoWrap;

    public double TitleFontSize => Height >= 52 ? 11 : 10;

    public double TimeFontSize => Height >= 44 ? 10 : 9;

    public double ContentOpacity => Task.ContentOpacity;
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WinCalendar.Modules.Planner.Presentation;

namespace WinCalendar.Modules.Planner.UI;

public sealed class CompactPlannerWindow : Window
{
    public CompactPlannerWindow(PlannerStateStore plannerStateStore)
    {
        Title = "WinCalendar";
        SystemBackdrop = new DesktopAcrylicBackdrop();
        Content = new CompactPlannerPage(plannerStateStore);
    }
}

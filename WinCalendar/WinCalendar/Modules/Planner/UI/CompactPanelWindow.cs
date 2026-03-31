using Microsoft.UI.Xaml;
using WinCalendar.Modules.Planner.Presentation;
using WinCalendar.Shared.Windowing;

namespace WinCalendar.Modules.Planner.UI;

public sealed class CompactPanelWindow : Window
{
    public CompactPanelWindow(PlannerStateStore plannerStateStore, PlannerWindowCoordinator windowCoordinator)
    {
        Title = "Planner Compact";
        Content = new CompactPanelPage(plannerStateStore, windowCoordinator);
    }
}

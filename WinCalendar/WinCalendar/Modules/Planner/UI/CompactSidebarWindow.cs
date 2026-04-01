using Microsoft.UI.Xaml;
using WinCalendar.Modules.Planner.Presentation;
using WinCalendar.Shared.Windowing;

namespace WinCalendar.Modules.Planner.UI;

public sealed class CompactSidebarWindow : Window
{
    public CompactSidebarWindow(PlannerStateStore plannerStateStore, PlannerWindowCoordinator windowCoordinator)
    {
        Title = "Planner Sidebar";
        Content = new CompactSidebarPage(plannerStateStore, windowCoordinator);
    }
}

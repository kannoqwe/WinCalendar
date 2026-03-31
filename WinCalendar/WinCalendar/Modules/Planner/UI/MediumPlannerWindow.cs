using Microsoft.UI.Xaml;
using WinCalendar.Modules.Planner.Presentation;
using WinCalendar.Shared.Windowing;

namespace WinCalendar.Modules.Planner.UI;

public sealed class MediumPlannerWindow : Window
{
    public MediumPlannerWindow(PlannerStateStore plannerStateStore, PlannerWindowCoordinator windowCoordinator)
    {
        Title = "Planner Day View";
        Content = new MediumPlannerPage(plannerStateStore, windowCoordinator);
    }
}

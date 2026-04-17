using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WinCalendar.Modules.Planner.Presentation;

namespace WinCalendar.Modules.Planner.UI;

public sealed class CompactPanelWindow : Window
{
    public CompactPanelWindow(PlannerStateStore plannerStateStore)
    {
        Title = "Planner Compact";
        SystemBackdrop = new DesktopAcrylicBackdrop();
        Content = new CompactPanelPage(plannerStateStore);
    }
}

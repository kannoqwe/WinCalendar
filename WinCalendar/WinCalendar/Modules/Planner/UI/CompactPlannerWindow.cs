using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WinCalendar.Modules.Planner.Presentation;

namespace WinCalendar.Modules.Planner.UI;

public sealed class CompactPlannerWindow : Window
{
    private readonly CompactPlannerPage _page;

    public CompactPlannerWindow(PlannerStateStore plannerStateStore, Action openMainApp)
    {
        Title = "WinCalendar";
        SystemBackdrop = new DesktopAcrylicBackdrop();
        _page = new CompactPlannerPage(plannerStateStore, openMainApp);
        Content = _page;
    }

    public Task PrepareForShowAsync() => _page.PrepareForShowAsync();

    public void PlayOpenAnimation() => _page.PlayOpenAnimation();
}

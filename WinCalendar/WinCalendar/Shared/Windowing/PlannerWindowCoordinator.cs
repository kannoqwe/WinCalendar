using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinCalendar.Modules.Planner.Presentation;
using WinCalendar.Modules.Planner.UI;

namespace WinCalendar.Shared.Windowing;

public sealed class PlannerWindowCoordinator
{
    private readonly PlannerStateStore _plannerStateStore;
    private MainWindow? _mainWindow;
    private CompactPanelWindow? _compactWindow;
    private MediumPlannerWindow? _mediumWindow;

    public PlannerWindowCoordinator(PlannerStateStore plannerStateStore)
    {
        _plannerStateStore = plannerStateStore;
    }

    public void AttachMainWindow(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public void ShowFullApp()
    {
        _mainWindow?.Activate();
    }

    public void ShowCompactPanel()
    {
        if (_compactWindow is null)
        {
            _compactWindow = new CompactPanelWindow(_plannerStateStore, this);
            _compactWindow.Closed += (_, _) => _compactWindow = null;
            ConfigureCompactWindow(_compactWindow);
        }

        _compactWindow.Activate();
    }

    public void ShowMediumView()
    {
        if (_mediumWindow is null)
        {
            _mediumWindow = new MediumPlannerWindow(_plannerStateStore, this);
            _mediumWindow.Closed += (_, _) => _mediumWindow = null;
            ConfigureMediumWindow(_mediumWindow);
        }

        _mediumWindow.Activate();
    }

    private static void ConfigureCompactWindow(Window window)
    {
        AppWindow appWindow = window.GetAppWindow();
        OverlappedPresenter presenter = OverlappedPresenter.CreateForToolWindow();
        presenter.IsAlwaysOnTop = true;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsResizable = false;

        appWindow.SetPresenter(presenter);
        appWindow.Resize(new SizeInt32(430, 640));
        PositionCompactWindow(appWindow);
    }

    private static void ConfigureMediumWindow(Window window)
    {
        AppWindow appWindow = window.GetAppWindow();
        OverlappedPresenter presenter = OverlappedPresenter.Create();
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;

        appWindow.SetPresenter(presenter);
        appWindow.Resize(new SizeInt32(980, 760));
        PositionMediumWindow(appWindow);
    }

    private static void PositionCompactWindow(AppWindow appWindow)
    {
        DisplayArea displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
        RectInt32 workArea = displayArea.WorkArea;
        const int margin = 18;
        const int width = 430;
        const int height = 640;

        appWindow.Move(new PointInt32(
            workArea.X + workArea.Width - width - margin,
            workArea.Y + workArea.Height - height - margin));
    }

    private static void PositionMediumWindow(AppWindow appWindow)
    {
        DisplayArea displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
        RectInt32 workArea = displayArea.WorkArea;
        const int width = 980;
        const int height = 760;

        appWindow.Move(new PointInt32(
            workArea.X + Math.Max(0, (workArea.Width - width) / 2),
            workArea.Y + Math.Max(0, (workArea.Height - height) / 2)));
    }
}

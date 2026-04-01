using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinCalendar.Modules.Planner.Presentation;
using WinCalendar.Modules.Planner.UI;

namespace WinCalendar.Shared.Windowing;

public sealed class PlannerWindowCoordinator
{
    private const int CompactWindowWidth = 420;
    private const int CompactWindowHeight = 500;
    private const int MediumWindowWidth = 980;
    private const int MediumWindowHeight = 760;

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
        presenter.SetBorderAndTitleBar(false, false);

        appWindow.SetPresenter(presenter);
        appWindow.Resize(new SizeInt32(CompactWindowWidth, CompactWindowHeight));
        PositionCompactWindow(appWindow);
    }

    private static void ConfigureMediumWindow(Window window)
    {
        AppWindow appWindow = window.GetAppWindow();
        OverlappedPresenter presenter = OverlappedPresenter.Create();
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;

        appWindow.SetPresenter(presenter);
        appWindow.Resize(new SizeInt32(MediumWindowWidth, MediumWindowHeight));
        PositionMediumWindow(appWindow);
    }

    private static void PositionCompactWindow(AppWindow appWindow)
    {
        DisplayArea displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
        RectInt32 workArea = displayArea.WorkArea;
        const int margin = 18;

        appWindow.Move(new PointInt32(
            workArea.X + workArea.Width - CompactWindowWidth - margin,
            workArea.Y + workArea.Height - CompactWindowHeight - margin));
    }

    private static void PositionMediumWindow(AppWindow appWindow)
    {
        DisplayArea displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
        RectInt32 workArea = displayArea.WorkArea;

        appWindow.Move(new PointInt32(
            workArea.X + Math.Max(0, (workArea.Width - MediumWindowWidth) / 2),
            workArea.Y + Math.Max(0, (workArea.Height - MediumWindowHeight) / 2)));
    }
}

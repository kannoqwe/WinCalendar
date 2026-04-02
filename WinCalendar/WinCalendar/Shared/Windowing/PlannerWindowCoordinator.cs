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
    private const int CompactSidebarWindowWidth = 228;
    private const int MediumWindowWidth = 980;
    private const int MediumWindowHeight = 760;

    private readonly PlannerStateStore _plannerStateStore;
    private MainWindow? _mainWindow;
    private CompactPanelWindow? _compactWindow;
    private CompactSidebarWindow? _compactSidebarWindow;
    private MediumPlannerWindow? _mediumWindow;
    private bool _suppressCompactSidebarClosedStateUpdate;

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
            _compactWindow.Closed += (_, _) =>
            {
                _compactWindow = null;
                CloseCompactSidebar(updateState: false);
            };
            ConfigureCompactWindow(_compactWindow);
        }
        else
        {
            PositionCompactWindow(_compactWindow.GetAppWindow());
        }

        if (_plannerStateStore.IsCompactSidebarOpen)
            ShowCompactSidebar();

        _compactWindow.Activate();
    }

    public void ToggleCompactSidebar()
    {
        if (_plannerStateStore.IsCompactSidebarOpen)
        {
            CloseCompactSidebar(updateState: true);
            _compactWindow?.Activate();
            return;
        }

        _plannerStateStore.SetCompactSidebarOpen(true);
        ShowCompactPanel();
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

    private void ShowCompactSidebar()
    {
        if (_compactWindow is null)
            return;

        if (_compactSidebarWindow is null)
        {
            _compactSidebarWindow = new CompactSidebarWindow(_plannerStateStore, this);
            _compactSidebarWindow.Closed += (_, _) =>
            {
                _compactSidebarWindow = null;

                if (_suppressCompactSidebarClosedStateUpdate)
                {
                    _suppressCompactSidebarClosedStateUpdate = false;
                    return;
                }

                _plannerStateStore.SetCompactSidebarOpen(false);
            };
            ConfigureCompactSidebarWindow(_compactSidebarWindow);
        }
        else
        {
            ConfigureCompactSidebarWindow(_compactSidebarWindow);
        }

        _compactSidebarWindow.Activate();
    }

    private void CloseCompactSidebar(bool updateState)
    {
        if (updateState)
            _plannerStateStore.SetCompactSidebarOpen(false);

        if (_compactSidebarWindow is null)
            return;

        _suppressCompactSidebarClosedStateUpdate = true;
        _compactSidebarWindow.Close();
    }

    private static void ConfigureCompactSidebarWindow(Window window)
    {
        AppWindow appWindow = window.GetAppWindow();
        OverlappedPresenter presenter = OverlappedPresenter.CreateForToolWindow();
        presenter.IsAlwaysOnTop = true;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsResizable = false;
        presenter.SetBorderAndTitleBar(false, false);

        appWindow.SetPresenter(presenter);
        appWindow.Resize(new SizeInt32(CompactSidebarWindowWidth, CompactWindowHeight));
        PositionCompactSidebarWindow(appWindow);
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

    private static void PositionCompactSidebarWindow(AppWindow appWindow)
    {
        DisplayArea displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
        RectInt32 workArea = displayArea.WorkArea;
        const int margin = 18;
        int compactWindowX = workArea.X + workArea.Width - CompactWindowWidth - margin;
        int compactWindowY = workArea.Y + workArea.Height - CompactWindowHeight - margin;
        int sidebarX = Math.Max(workArea.X + margin, compactWindowX - CompactSidebarWindowWidth);

        appWindow.Move(new PointInt32(sidebarX, compactWindowY));
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

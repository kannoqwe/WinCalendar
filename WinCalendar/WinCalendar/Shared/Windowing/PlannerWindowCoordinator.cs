using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinCalendar.Modules.Planner.Presentation;
using WinCalendar.Modules.Planner.UI;

namespace WinCalendar.Shared.Windowing;

public sealed class PlannerWindowCoordinator
{
    private const int MainWindowWidth = 1360;
    private const int MainWindowHeight = 860;
    private const int CompactWindowWidth = 360;
    private const int CompactWindowHeight = 548;
    private const int CompactWindowCornerRadius = 12;
    private const int MediumWindowWidth = 980;
    private const int MediumWindowHeight = 760;

    private readonly PlannerStateStore _plannerStateStore;
    private readonly Func<MainWindow> _mainWindowFactory;
    private MainWindow? _mainWindow;
    private CompactPanelWindow? _compactWindow;
    private MediumPlannerWindow? _mediumWindow;

    public PlannerWindowCoordinator(PlannerStateStore plannerStateStore, Func<MainWindow> mainWindowFactory)
    {
        _plannerStateStore = plannerStateStore;
        _mainWindowFactory = mainWindowFactory;
    }

    public void AttachMainWindow(MainWindow mainWindow)
    {
        if (_mainWindow is not null)
            _mainWindow.Closed -= MainWindow_Closed;

        _mainWindow = mainWindow;
        _mainWindow.Closed += MainWindow_Closed;
        ConfigureMainWindow(_mainWindow);
    }

    public void ShowFullApp()
    {
        if (_compactWindow is not null)
        {
            _compactWindow.Close();
            _compactWindow = null;
        }

        if (_mainWindow is null)
            _mainWindow = _mainWindowFactory();

        _mainWindow.Activate();
    }

    public void ShowCompactPanel()
    {
        if (_compactWindow is null)
        {
            _compactWindow = new CompactPanelWindow(_plannerStateStore);
            _compactWindow.Closed += (_, _) =>
            {
                _compactWindow = null;
            };
            ConfigureCompactWindow(_compactWindow);
        }
        else
        {
            ConfigureCompactWindow(_compactWindow);
        }

        _compactWindow.Activate();
    }

    public void ToggleCompactPanel()
    {
        if (_compactWindow is not null)
        {
            _compactWindow.Close();
            return;
        }

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

    private void ConfigureCompactWindow(Window window)
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
        TransparentWindowHost.Apply(
            window,
            CompactWindowWidth,
            CompactWindowHeight,
            CompactWindowCornerRadius,
            TransparentWindowHost.WindowOutlineShape.AllRounded);
    }

    private static void ConfigureMainWindow(Window window)
    {
        AppWindow appWindow = window.GetAppWindow();
        appWindow.Resize(new SizeInt32(MainWindowWidth, MainWindowHeight));
        PositionMainWindow(appWindow);
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

    private static void PositionMainWindow(AppWindow appWindow)
    {
        DisplayArea displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
        RectInt32 workArea = displayArea.WorkArea;

        appWindow.Move(new PointInt32(
            workArea.X + Math.Max(0, (workArea.Width - MainWindowWidth) / 2),
            workArea.Y + Math.Max(0, (workArea.Height - MainWindowHeight) / 2)));
    }

    private static void PositionMediumWindow(AppWindow appWindow)
    {
        DisplayArea displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
        RectInt32 workArea = displayArea.WorkArea;

        appWindow.Move(new PointInt32(
            workArea.X + Math.Max(0, (workArea.Width - MediumWindowWidth) / 2),
            workArea.Y + Math.Max(0, (workArea.Height - MediumWindowHeight) / 2)));
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        if (sender is not MainWindow mainWindow || !ReferenceEquals(_mainWindow, mainWindow))
            return;

        _mainWindow.Closed -= MainWindow_Closed;
        _mainWindow = null;
    }
}

using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinCalendar.Modules.Planner.Presentation;
using WinCalendar.Modules.Planner.UI;
using WinRT.Interop;

namespace WinCalendar.Shared.Windowing;

public sealed class PlannerWindowCoordinator
{
    private const int DipsPerInch = 96;
    private const int MainWindowWidth = 1360;
    private const int MainWindowHeight = 860;
    private const int MediumWindowWidth = 980;
    private const int MediumWindowHeight = 760;
    private const int CompactWindowWidth = 360;
    private const int CompactWindowHeight = 536;
    private const int CompactWindowMargin = 12;

    private readonly PlannerStateStore _plannerStateStore;
    private readonly Func<MainWindow> _mainWindowFactory;
    private MainWindow? _mainWindow;
    private MediumPlannerWindow? _mediumWindow;
    private CompactPlannerWindow? _compactWindow;

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
        if (_mainWindow is null)
            _mainWindow = _mainWindowFactory();

        _mainWindow.Activate();
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

    public void ToggleCompactPanel()
    {
        if (_compactWindow is not null)
        {
            _compactWindow.Close();
            return;
        }

        _compactWindow = new CompactPlannerWindow(_plannerStateStore);
        _compactWindow.Closed += CompactWindow_Closed;
        ConfigureCompactWindow(_compactWindow);
        _compactWindow.Activate();
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

    private static void ConfigureCompactWindow(Window window)
    {
        AppWindow appWindow = window.GetAppWindow();
        int width = ScaleDipToPixel(window, CompactWindowWidth);
        int height = ScaleDipToPixel(window, CompactWindowHeight);
        int margin = ScaleDipToPixel(window, CompactWindowMargin);
        int cornerRadius = ScaleDipToPixel(window, 12);

        OverlappedPresenter presenter = OverlappedPresenter.CreateForToolWindow();
        presenter.IsAlwaysOnTop = true;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsResizable = false;
        presenter.SetBorderAndTitleBar(false, false);

        appWindow.SetPresenter(presenter);
        appWindow.Resize(new SizeInt32(width, height));
        PositionCompactWindow(appWindow, width, height, margin);
        TransparentWindowHost.Apply(
            window,
            width,
            height,
            cornerRadius,
            TransparentWindowHost.WindowOutlineShape.AllRounded);
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

    private static void PositionCompactWindow(AppWindow appWindow, int width, int height, int margin)
    {
        DisplayArea displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
        RectInt32 workArea = displayArea.WorkArea;

        appWindow.Move(new PointInt32(
            workArea.X + Math.Max(0, workArea.Width - width - margin),
            workArea.Y + Math.Max(0, workArea.Height - height - margin)));
    }

    private static int ScaleDipToPixel(Window window, int value)
    {
        uint dpi = GetWindowDpi(window);
        return Math.Max(1, (int)Math.Round(value * dpi / (double)DipsPerInch));
    }

    private static uint GetWindowDpi(Window window)
    {
        nint windowHandle = WindowNative.GetWindowHandle(window);
        if (windowHandle == nint.Zero)
            return DipsPerInch;

        uint dpi = GetDpiForWindow(windowHandle);
        return dpi == 0 ? DipsPerInch : dpi;
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        if (sender is not MainWindow mainWindow || !ReferenceEquals(_mainWindow, mainWindow))
            return;

        _mainWindow.Closed -= MainWindow_Closed;
        _mainWindow = null;
    }

    private void CompactWindow_Closed(object sender, WindowEventArgs args)
    {
        if (sender is not CompactPlannerWindow compactWindow || !ReferenceEquals(_compactWindow, compactWindow))
            return;

        _compactWindow.Closed -= CompactWindow_Closed;
        _compactWindow = null;
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint hwnd);
}

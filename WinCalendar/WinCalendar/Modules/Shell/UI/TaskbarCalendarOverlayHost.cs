using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using WinCalendar.Modules.Shell.Infrastructure.Win32;

namespace WinCalendar.Modules.Shell.UI;

internal sealed class TaskbarCalendarOverlayHost : IDisposable
{
    private const int OverlayWidth = 140;
    private const int OverlayHeight = 52;
    private const int OverlayRightMargin = 4;
    private const int OverlayBottomMargin = 0;
    private const int SmCxScreen = 0;
    private const int SmCyScreen = 1;

    private readonly Action _onClick;
    private readonly DispatcherQueueTimer _syncTimer;
    private readonly TrayNative.WndProc _windowProcedure;
    private readonly string _windowClassName;
    private readonly nint _moduleHandle;
    private nint _overlayWindowHandle;
    private bool _isDisposed;

    public TaskbarCalendarOverlayHost(DispatcherQueue dispatcherQueue, Action onClick)
    {
        _onClick = onClick;
        _windowProcedure = WindowProcedure;
        _windowClassName = $"WinCalendar.TaskbarCalendarOverlay.{Environment.ProcessId}";
        _moduleHandle = TrayNative.GetModuleHandle(lpModuleName: null);

        RegisterWindowClass();
        CreateOverlayWindow();

        _syncTimer = dispatcherQueue.CreateTimer();
        _syncTimer.Interval = TimeSpan.FromSeconds(15);
        _syncTimer.IsRepeating = true;
        _syncTimer.Tick += SyncTimer_Tick;
        _syncTimer.Start();
    }

    private void RegisterWindowClass()
    {
        TrayNative.WNDCLASSEX windowClass = new()
        {
            cbSize = (uint)Marshal.SizeOf<TrayNative.WNDCLASSEX>(),
            lpfnWndProc = _windowProcedure,
            hInstance = _moduleHandle,
            lpszClassName = _windowClassName
        };

        ushort classAtom = TrayNative.RegisterClassEx(ref windowClass);
        if (classAtom == 0)
            throw new InvalidOperationException("Unable to register WinCalendar taskbar calendar overlay class.");
    }

    private void CreateOverlayWindow()
    {
        Position overlayPosition = GetOverlayPosition();
        _overlayWindowHandle = TrayNative.CreateWindowEx(
            dwExStyle: TrayNative.WS_EX_TOPMOST
                | TrayNative.WS_EX_TOOLWINDOW
                | TrayNative.WS_EX_LAYERED
                | TrayNative.WS_EX_NOACTIVATE,
            lpClassName: _windowClassName,
            lpWindowName: _windowClassName,
            dwStyle: TrayNative.WS_POPUP,
            x: overlayPosition.X,
            y: overlayPosition.Y,
            nWidth: OverlayWidth,
            nHeight: OverlayHeight,
            hWndParent: nint.Zero,
            hMenu: nint.Zero,
            hInstance: _moduleHandle,
            lpParam: nint.Zero);

        if (_overlayWindowHandle == nint.Zero)
            throw new InvalidOperationException("Unable to create WinCalendar taskbar calendar overlay.");

        _ = TrayNative.SetLayeredWindowAttributes(
            _overlayWindowHandle,
            crKey: 0,
            bAlpha: 1,
            dwFlags: TrayNative.LWA_ALPHA);

        SyncOverlayWindow();
    }

    private void SyncTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        SyncOverlayWindow();
    }

    private void SyncOverlayWindow()
    {
        if (_isDisposed || _overlayWindowHandle == nint.Zero)
            return;

        Position overlayPosition = GetOverlayPosition();
        _ = TrayNative.SetWindowPos(
            _overlayWindowHandle,
            TrayNative.HwndTopMost,
            overlayPosition.X,
            overlayPosition.Y,
            OverlayWidth,
            OverlayHeight,
            TrayNative.SWP_NOACTIVATE | TrayNative.SWP_SHOWWINDOW);

        _ = TrayNative.ShowWindow(_overlayWindowHandle, TrayNative.SW_SHOWNOACTIVATE);
    }

    private nint WindowProcedure(nint hWnd, uint msg, nuint wParam, nint lParam)
    {
        switch (msg)
        {
            case TrayNative.WM_MOUSEACTIVATE:
                return new nint(TrayNative.MA_NOACTIVATE);
            case TrayNative.WM_NCHITTEST:
                return new nint(TrayNative.HTCLIENT);
            case TrayNative.WM_LBUTTONDOWN:
                _onClick();
                return nint.Zero;
            default:
                return TrayNative.DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }

    private static Position GetOverlayPosition()
    {
        int screenWidth = GetSystemMetrics(SmCxScreen);
        int screenHeight = GetSystemMetrics(SmCyScreen);

        return new Position(
            Math.Max(0, screenWidth - OverlayWidth - OverlayRightMargin),
            Math.Max(0, screenHeight - OverlayHeight - OverlayBottomMargin));
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _syncTimer.Stop();
        _syncTimer.Tick -= SyncTimer_Tick;

        if (_overlayWindowHandle != nint.Zero)
        {
            TrayNative.DestroyWindow(_overlayWindowHandle);
            _overlayWindowHandle = nint.Zero;
        }

        TrayNative.UnregisterClass(_windowClassName, _moduleHandle);
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private readonly record struct Position(int X, int Y);
}

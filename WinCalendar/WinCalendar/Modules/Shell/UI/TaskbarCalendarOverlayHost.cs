using System;
using System.Runtime.InteropServices;
using System.Threading;
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
    private const uint MonitorDefaultToNearest = 2;
    private const int FullscreenTolerance = 2;
    private const int ObjidWindow = 0;
    private const int ChildIdSelf = 0;
    private const uint EventSystemForeground = 0x0003;
    private const uint EventObjectShow = 0x8002;
    private const uint EventObjectReorder = 0x8004;
    private const uint EventObjectLocationChange = 0x800B;
    private const uint WinEventOutOfContext = 0x0000;
    private const uint WinEventSkipOwnProcess = 0x0002;

    private readonly Action _onClick;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueTimer _syncTimer;
    private readonly TrayNative.WndProc _windowProcedure;
    private readonly WinEventProc _winEventProcedure;
    private readonly string _windowClassName;
    private readonly nint _moduleHandle;
    private nint _overlayWindowHandle;
    private nint _foregroundHookHandle;
    private nint _windowOrderHookHandle;
    private nint _windowLocationHookHandle;
    private int _syncQueued;
    private bool _isDisposed;

    public TaskbarCalendarOverlayHost(DispatcherQueue dispatcherQueue, Action onClick)
    {
        _onClick = onClick;
        _dispatcherQueue = dispatcherQueue;
        _windowProcedure = WindowProcedure;
        _winEventProcedure = WinEventProcedure;
        _windowClassName = $"WinCalendar.TaskbarCalendarOverlay.{Environment.ProcessId}";
        _moduleHandle = TrayNative.GetModuleHandle(lpModuleName: null);

        RegisterWindowClass();
        CreateOverlayWindow();
        RegisterWinEventHooks();

        _syncTimer = dispatcherQueue.CreateTimer();
        _syncTimer.Interval = TimeSpan.FromSeconds(10);
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

    private void RegisterWinEventHooks()
    {
        const uint flags = WinEventOutOfContext | WinEventSkipOwnProcess;

        _foregroundHookHandle = SetWinEventHook(
            EventSystemForeground,
            EventSystemForeground,
            nint.Zero,
            _winEventProcedure,
            0,
            0,
            flags);

        _windowOrderHookHandle = SetWinEventHook(
            EventObjectShow,
            EventObjectReorder,
            nint.Zero,
            _winEventProcedure,
            0,
            0,
            flags);

        _windowLocationHookHandle = SetWinEventHook(
            EventObjectLocationChange,
            EventObjectLocationChange,
            nint.Zero,
            _winEventProcedure,
            0,
            0,
            flags);
    }

    private void WinEventProcedure(
        nint winEventHook,
        uint eventType,
        nint hwnd,
        int idObject,
        int idChild,
        uint idEventThread,
        uint eventTime)
    {
        if (_isDisposed || hwnd == _overlayWindowHandle)
            return;

        if (eventType != EventSystemForeground
            && (hwnd == nint.Zero || idObject != ObjidWindow || idChild != ChildIdSelf))
        {
            return;
        }

        QueueSyncOverlayWindow();
    }

    private void QueueSyncOverlayWindow()
    {
        if (_isDisposed || Interlocked.Exchange(ref _syncQueued, 1) == 1)
            return;

        bool queued = _dispatcherQueue.TryEnqueue(() =>
        {
            Interlocked.Exchange(ref _syncQueued, 0);
            SyncOverlayWindow();
        });

        if (!queued)
            Interlocked.Exchange(ref _syncQueued, 0);
    }

    private void SyncOverlayWindow()
    {
        if (_isDisposed || _overlayWindowHandle == nint.Zero)
            return;

        if (IsForegroundWindowFullscreen())
        {
            _ = TrayNative.ShowWindow(_overlayWindowHandle, TrayNative.SW_HIDE);
            return;
        }

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

    private bool IsForegroundWindowFullscreen()
    {
        nint foregroundWindowHandle = GetForegroundWindow();
        if (foregroundWindowHandle == nint.Zero || foregroundWindowHandle == _overlayWindowHandle)
            return false;

        if (!GetWindowRect(foregroundWindowHandle, out RECT windowRect))
            return false;

        nint monitorHandle = MonitorFromWindow(foregroundWindowHandle, MonitorDefaultToNearest);
        if (monitorHandle == nint.Zero)
            return false;

        MONITORINFO monitorInfo = new()
        {
            cbSize = Marshal.SizeOf<MONITORINFO>()
        };

        if (!GetMonitorInfo(monitorHandle, ref monitorInfo))
            return false;

        RECT monitorRect = monitorInfo.rcMonitor;
        return windowRect.Left <= monitorRect.Left + FullscreenTolerance
            && windowRect.Top <= monitorRect.Top + FullscreenTolerance
            && windowRect.Right >= monitorRect.Right - FullscreenTolerance
            && windowRect.Bottom >= monitorRect.Bottom - FullscreenTolerance;
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
        UnregisterWinEventHooks();
        _syncTimer.Stop();
        _syncTimer.Tick -= SyncTimer_Tick;

        if (_overlayWindowHandle != nint.Zero)
        {
            TrayNative.DestroyWindow(_overlayWindowHandle);
            _overlayWindowHandle = nint.Zero;
        }

        TrayNative.UnregisterClass(_windowClassName, _moduleHandle);
    }

    private void UnregisterWinEventHooks()
    {
        if (_foregroundHookHandle != nint.Zero)
        {
            UnhookWinEvent(_foregroundHookHandle);
            _foregroundHookHandle = nint.Zero;
        }

        if (_windowOrderHookHandle != nint.Zero)
        {
            UnhookWinEvent(_windowOrderHookHandle);
            _windowOrderHookHandle = nint.Zero;
        }

        if (_windowLocationHookHandle != nint.Zero)
        {
            UnhookWinEvent(_windowLocationHookHandle);
            _windowLocationHookHandle = nint.Zero;
        }
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWinEventHook(
        uint eventMin,
        uint eventMax,
        nint hmodWinEventProc,
        WinEventProc lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWinEvent(nint hWinEventHook);

    private readonly record struct Position(int X, int Y);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate void WinEventProc(
        nint hWinEventHook,
        uint eventType,
        nint hwnd,
        int idObject,
        int idChild,
        uint idEventThread,
        uint eventTime);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }
}

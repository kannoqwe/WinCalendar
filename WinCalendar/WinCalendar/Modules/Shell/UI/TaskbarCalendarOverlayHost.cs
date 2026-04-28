using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.UI.Dispatching;
using WinCalendar.Modules.Shell.Infrastructure.Win32;
using WinCalendar.Shared.Settings;

namespace WinCalendar.Modules.Shell.UI;

internal sealed class TaskbarCalendarOverlayHost : IDisposable
{
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
    private const int EnterHotKeyId = 1;
    private const int EscapeHotKeyId = 2;

    private readonly Action _onClick;
    private readonly AppSettingsStore _appSettingsStore;
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
    private bool _isResizeMode;
    private bool _isSizing;
    private OverlayBounds _previewBounds;
    private DragOperation _dragOperation;
    private DragState _dragState;

    public TaskbarCalendarOverlayHost(
        DispatcherQueue dispatcherQueue,
        AppSettingsStore appSettingsStore,
        Action onClick)
    {
        _onClick = onClick;
        _appSettingsStore = appSettingsStore;
        _dispatcherQueue = dispatcherQueue;
        _windowProcedure = WindowProcedure;
        _winEventProcedure = WinEventProcedure;
        _windowClassName = $"WinCalendar.TaskbarCalendarOverlay.{Environment.ProcessId}";
        _moduleHandle = TrayNative.GetModuleHandle(lpModuleName: null);
        _previewBounds = GetStoredOverlayBounds();

        RegisterWindowClass();
        CreateOverlayWindow();
        RegisterWinEventHooks();
        _appSettingsStore.OverlaySettingsChanged += AppSettingsStore_OverlaySettingsChanged;

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
            nWidth: GetOverlayWidth(),
            nHeight: GetOverlayHeight(),
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

    public void ToggleResizeMode()
    {
        if (_isDisposed || _overlayWindowHandle == nint.Zero)
            return;

        if (_isResizeMode)
        {
            EndResizeMode();
            return;
        }

        _isResizeMode = true;
        RegisterResizeHotKeys();
        _ = TrayNative.SetLayeredWindowAttributes(
            _overlayWindowHandle,
            crKey: 0,
            bAlpha: 180,
            dwFlags: TrayNative.LWA_ALPHA);

        SyncOverlayWindow();
        InvalidateRect(_overlayWindowHandle, nint.Zero, true);
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

        if (!_isResizeMode && IsForegroundWindowFullscreen())
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
            GetOverlayWidth(),
            GetOverlayHeight(),
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
            case TrayNative.WM_PAINT:
                PaintOverlay(hWnd);
                return nint.Zero;
            case TrayNative.WM_HOTKEY:
                if (_isResizeMode && (wParam == (nuint)EnterHotKeyId || wParam == (nuint)EscapeHotKeyId))
                {
                    if (_isSizing)
                    {
                        EndSizing();
                    }
                    else
                    {
                        EndResizeMode();
                    }

                    return nint.Zero;
                }

                return TrayNative.DefWindowProc(hWnd, msg, wParam, lParam);
            case TrayNative.WM_MOUSEACTIVATE:
                return new nint(TrayNative.MA_NOACTIVATE);
            case TrayNative.WM_NCHITTEST:
                return new nint(GetHitTest());
            case TrayNative.WM_LBUTTONDOWN:
                if (_isResizeMode)
                {
                    BeginSizing();
                    return nint.Zero;
                }

                _onClick();
                return nint.Zero;
            case TrayNative.WM_RBUTTONDOWN:
                if (_isResizeMode)
                {
                    EndResizeMode();
                    return nint.Zero;
                }

                return TrayNative.DefWindowProc(hWnd, msg, wParam, lParam);
            case TrayNative.WM_MOUSEMOVE:
                if (_isSizing)
                    UpdateSizing();

                return nint.Zero;
            case TrayNative.WM_LBUTTONUP:
                if (_isSizing)
                {
                    EndSizing();
                    return nint.Zero;
                }

                return nint.Zero;
            default:
                return TrayNative.DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }

    private int GetHitTest()
    {
        return TrayNative.HTCLIENT;
    }

    private void BeginSizing()
    {
        Point point = GetCursorPoint();
        OverlayBounds bounds = GetCurrentOverlayBounds();
        _isSizing = true;
        _previewBounds = bounds;
        _dragOperation = GetDragOperation(point, bounds);
        _dragState = new DragState(
            point.X,
            point.Y,
            bounds);
        SetCapture(_overlayWindowHandle);
    }

    private void UpdateSizing()
    {
        Point point = GetCursorPoint();
        int deltaX = point.X - _dragState.ScreenX;
        int deltaY = point.Y - _dragState.ScreenY;
        int left = _dragState.Bounds.X;
        int top = _dragState.Bounds.Y;
        int right = _dragState.Bounds.X + _dragState.Bounds.Width;
        int bottom = _dragState.Bounds.Y + _dragState.Bounds.Height;

        if (_dragOperation == DragOperation.Move)
        {
            MovePreviewBounds(deltaX, deltaY);
        }
        else
        {
            if ((_dragOperation & DragOperation.Left) == DragOperation.Left)
                left += deltaX;

            if ((_dragOperation & DragOperation.Right) == DragOperation.Right)
                right += deltaX;

            if ((_dragOperation & DragOperation.Top) == DragOperation.Top)
                top += deltaY;

            if ((_dragOperation & DragOperation.Bottom) == DragOperation.Bottom)
                bottom += deltaY;

            NormalizePreviewBounds(left, top, right, bottom);
        }

        SyncOverlayWindow();
        InvalidateRect(_overlayWindowHandle, nint.Zero, true);
    }

    private void EndSizing()
    {
        _isSizing = false;
        _dragOperation = DragOperation.None;
        ReleaseCapture();
        int screenWidth = GetSystemMetrics(SmCxScreen);
        int screenHeight = GetSystemMetrics(SmCyScreen);
        _appSettingsStore.SetOverlayBounds(
            _previewBounds.Width,
            _previewBounds.Height,
            screenWidth - _previewBounds.X - _previewBounds.Width,
            screenHeight - _previewBounds.Y - _previewBounds.Height);
        EndResizeMode();
    }

    private void EndResizeMode()
    {
        _isResizeMode = false;
        UnregisterResizeHotKeys();
        _ = TrayNative.SetLayeredWindowAttributes(
            _overlayWindowHandle,
            crKey: 0,
            bAlpha: 1,
            dwFlags: TrayNative.LWA_ALPHA);
        SyncOverlayWindow();
    }

    private void PaintOverlay(nint windowHandle)
    {
        nint paintHandle = BeginPaint(windowHandle, out PAINTSTRUCT paintStruct);
        if (paintHandle != nint.Zero)
        {
            nint fillBrush = CreateSolidBrush(0x00E6B04F);
            nint borderBrush = CreateSolidBrush(0x000088FF);

            try
            {
                FillRect(paintStruct.hdc, ref paintStruct.rcPaint, fillBrush);
                RECT frameRect = new()
                {
                    Left = 0,
                    Top = 0,
                    Right = GetOverlayBounds().Width,
                    Bottom = GetOverlayBounds().Height
                };
                FrameRect(paintStruct.hdc, ref frameRect, borderBrush);
            }
            finally
            {
                DeleteObject(fillBrush);
                DeleteObject(borderBrush);
                EndPaint(windowHandle, ref paintStruct);
            }
        }
    }

    private void AppSettingsStore_OverlaySettingsChanged(object? sender, EventArgs e)
    {
        if (!_isSizing)
            _previewBounds = GetStoredOverlayBounds();

        SyncOverlayWindow();
    }

    private void RegisterResizeHotKeys()
    {
        RegisterHotKey(_overlayWindowHandle, EnterHotKeyId, 0, TrayNative.VK_RETURN);
        RegisterHotKey(_overlayWindowHandle, EscapeHotKeyId, 0, TrayNative.VK_ESCAPE);
    }

    private void UnregisterResizeHotKeys()
    {
        UnregisterHotKey(_overlayWindowHandle, EnterHotKeyId);
        UnregisterHotKey(_overlayWindowHandle, EscapeHotKeyId);
    }

    private Position GetOverlayPosition()
    {
        OverlayBounds bounds = GetOverlayBounds();
        return new Position(bounds.X, bounds.Y);
    }

    private int GetOverlayWidth() => GetOverlayBounds().Width;

    private int GetOverlayHeight() => GetOverlayBounds().Height;

    private OverlayBounds GetOverlayBounds() => _isResizeMode ? _previewBounds : GetStoredOverlayBounds();

    private OverlayBounds GetStoredOverlayBounds()
    {
        int screenWidth = GetSystemMetrics(SmCxScreen);
        int screenHeight = GetSystemMetrics(SmCyScreen);
        int width = _appSettingsStore.OverlayWidth;
        int height = _appSettingsStore.OverlayHeight;
        int x = Math.Clamp(
            screenWidth - width - _appSettingsStore.OverlayRightOffset,
            0,
            Math.Max(0, screenWidth - width));
        int y = Math.Clamp(
            screenHeight - height - _appSettingsStore.OverlayBottomOffset,
            0,
            Math.Max(0, screenHeight - height));

        return new OverlayBounds(x, y, width, height);
    }

    private OverlayBounds GetCurrentOverlayBounds()
    {
        if (GetWindowRect(_overlayWindowHandle, out RECT rect))
            return new OverlayBounds(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);

        return GetOverlayBounds();
    }

    private void NormalizePreviewBounds(int left, int top, int right, int bottom)
    {
        int screenWidth = GetSystemMetrics(SmCxScreen);
        int screenHeight = GetSystemMetrics(SmCyScreen);
        left = Math.Clamp(left, 0, screenWidth - AppSettingsStore.MinimumOverlayWidth);
        top = Math.Clamp(top, 0, screenHeight - AppSettingsStore.MinimumOverlayHeight);
        right = Math.Clamp(right, AppSettingsStore.MinimumOverlayWidth, screenWidth);
        bottom = Math.Clamp(bottom, AppSettingsStore.MinimumOverlayHeight, screenHeight);

        if (right - left < AppSettingsStore.MinimumOverlayWidth)
        {
            if ((_dragOperation & DragOperation.Left) == DragOperation.Left)
                left = right - AppSettingsStore.MinimumOverlayWidth;
            else
                right = left + AppSettingsStore.MinimumOverlayWidth;
        }

        if (bottom - top < AppSettingsStore.MinimumOverlayHeight)
        {
            if ((_dragOperation & DragOperation.Top) == DragOperation.Top)
                top = bottom - AppSettingsStore.MinimumOverlayHeight;
            else
                bottom = top + AppSettingsStore.MinimumOverlayHeight;
        }

        if (right - left > AppSettingsStore.MaximumOverlayWidth)
        {
            if ((_dragOperation & DragOperation.Left) == DragOperation.Left)
                left = right - AppSettingsStore.MaximumOverlayWidth;
            else
                right = left + AppSettingsStore.MaximumOverlayWidth;
        }

        if (bottom - top > AppSettingsStore.MaximumOverlayHeight)
        {
            if ((_dragOperation & DragOperation.Top) == DragOperation.Top)
                top = bottom - AppSettingsStore.MaximumOverlayHeight;
            else
                bottom = top + AppSettingsStore.MaximumOverlayHeight;
        }

        left = Math.Max(0, left);
        top = Math.Max(0, top);
        right = Math.Min(screenWidth, right);
        bottom = Math.Min(screenHeight, bottom);
        _previewBounds = new OverlayBounds(left, top, right - left, bottom - top);
    }

    private void MovePreviewBounds(int deltaX, int deltaY)
    {
        int screenWidth = GetSystemMetrics(SmCxScreen);
        int screenHeight = GetSystemMetrics(SmCyScreen);
        int x = Math.Clamp(
            _dragState.Bounds.X + deltaX,
            0,
            Math.Max(0, screenWidth - _dragState.Bounds.Width));
        int y = Math.Clamp(
            _dragState.Bounds.Y + deltaY,
            0,
            Math.Max(0, screenHeight - _dragState.Bounds.Height));

        _previewBounds = new OverlayBounds(x, y, _dragState.Bounds.Width, _dragState.Bounds.Height);
    }

    private static DragOperation GetDragOperation(Point point, OverlayBounds bounds)
    {
        const int edgeSize = 14;
        DragOperation operation = DragOperation.None;

        if (point.X <= bounds.X + edgeSize)
            operation |= DragOperation.Left;
        else if (point.X >= bounds.X + bounds.Width - edgeSize)
            operation |= DragOperation.Right;

        if (point.Y <= bounds.Y + edgeSize)
            operation |= DragOperation.Top;
        else if (point.Y >= bounds.Y + bounds.Height - edgeSize)
            operation |= DragOperation.Bottom;

        return operation == DragOperation.None ? DragOperation.Move : operation;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _appSettingsStore.OverlaySettingsChanged -= AppSettingsStore_OverlaySettingsChanged;
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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint BeginPaint(nint hWnd, out PAINTSTRUCT lpPaint);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EndPaint(nint hWnd, ref PAINTSTRUCT lpPaint);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FillRect(nint hDC, ref RECT lprc, nint hbr);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FrameRect(nint hDC, ref RECT lprc, nint hbr);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern nint CreateSolidBrush(uint color);

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(nint hObject);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool InvalidateRect(nint hWnd, nint lpRect, bool bErase);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetCapture(nint hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, int vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(nint hWnd, int id);

    private readonly record struct Position(int X, int Y);

    private readonly record struct Point(int X, int Y);

    private readonly record struct OverlayBounds(int X, int Y, int Width, int Height);

    private readonly record struct DragState(
        int ScreenX,
        int ScreenY,
        OverlayBounds Bounds);

    [Flags]
    private enum DragOperation
    {
        None = 0,
        Move = 1,
        Left = 2,
        Right = 4,
        Top = 8,
        Bottom = 16
    }

    private static Point GetCursorPoint()
    {
        return TrayNative.GetCursorPos(out TrayNative.POINT point)
            ? new Point(point.X, point.Y)
            : new Point(0, 0);
    }

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
    private struct PAINTSTRUCT
    {
        public nint hdc;
        public bool fErase;
        public RECT rcPaint;
        public bool fRestore;
        public bool fIncUpdate;
        public byte rgbReserved0;
        public byte rgbReserved1;
        public byte rgbReserved2;
        public byte rgbReserved3;
        public byte rgbReserved4;
        public byte rgbReserved5;
        public byte rgbReserved6;
        public byte rgbReserved7;
        public byte rgbReserved8;
        public byte rgbReserved9;
        public byte rgbReserved10;
        public byte rgbReserved11;
        public byte rgbReserved12;
        public byte rgbReserved13;
        public byte rgbReserved14;
        public byte rgbReserved15;
        public byte rgbReserved16;
        public byte rgbReserved17;
        public byte rgbReserved18;
        public byte rgbReserved19;
        public byte rgbReserved20;
        public byte rgbReserved21;
        public byte rgbReserved22;
        public byte rgbReserved23;
        public byte rgbReserved24;
        public byte rgbReserved25;
        public byte rgbReserved26;
        public byte rgbReserved27;
        public byte rgbReserved28;
        public byte rgbReserved29;
        public byte rgbReserved30;
        public byte rgbReserved31;
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

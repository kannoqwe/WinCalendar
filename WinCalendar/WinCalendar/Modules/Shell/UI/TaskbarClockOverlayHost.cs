using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using WinCalendar.Modules.Shell.Infrastructure.Win32;

namespace WinCalendar.Modules.Shell.UI;

internal sealed class TaskbarClockOverlayHost : IDisposable
{
    private readonly Action _onClockClick;
    private readonly TaskbarClockBoundsProvider _boundsProvider;
    private readonly DispatcherQueueTimer _syncTimer;
    private readonly List<TransparentClockOverlayWindow> _overlayWindows = [];
    private bool _isDisposed;

    public TaskbarClockOverlayHost(DispatcherQueue dispatcherQueue, Action onClockClick)
    {
        _onClockClick = onClockClick;
        _boundsProvider = new TaskbarClockBoundsProvider();
        _syncTimer = dispatcherQueue.CreateTimer();
        _syncTimer.Interval = TimeSpan.FromSeconds(1);
        _syncTimer.IsRepeating = true;
        _syncTimer.Tick += SyncTimer_Tick;
        SyncOverlayWindows();
        _syncTimer.Start();
    }

    private void SyncTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        SyncOverlayWindows();
    }

    private void SyncOverlayWindows()
    {
        IReadOnlyList<TaskbarNative.RECT> bounds = _boundsProvider.GetClockBounds();

        while (_overlayWindows.Count < bounds.Count)
            _overlayWindows.Add(new TransparentClockOverlayWindow(_onClockClick));

        for (int index = 0; index < _overlayWindows.Count; index++)
        {
            if (index < bounds.Count)
            {
                _overlayWindows[index].Show(bounds[index]);
                continue;
            }

            _overlayWindows[index].Hide();
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _syncTimer.Stop();
        _syncTimer.Tick -= SyncTimer_Tick;

        foreach (TransparentClockOverlayWindow overlayWindow in _overlayWindows)
            overlayWindow.Dispose();

        _overlayWindows.Clear();
    }

    private sealed class TransparentClockOverlayWindow : IDisposable
    {
        private readonly Action _onClick;
        private readonly TrayNative.WndProc _windowProcedure;
        private readonly string _windowClassName;
        private readonly nint _moduleHandle;
        private nint _windowHandle;
        private bool _isDisposed;

        public TransparentClockOverlayWindow(Action onClick)
        {
            _onClick = onClick;
            _windowProcedure = WindowProcedure;
            _windowClassName = $"WinCalendar.ClockOverlay.{Environment.ProcessId}.{Guid.NewGuid():N}";
            _moduleHandle = TrayNative.GetModuleHandle(lpModuleName: null);

            RegisterWindowClass();
            CreateOverlayWindow();
        }

        public void Show(TaskbarNative.RECT bounds)
        {
            if (_windowHandle == nint.Zero || !IsValidBounds(bounds))
                return;

            int width = bounds.Right - bounds.Left;
            int height = bounds.Bottom - bounds.Top;

            TrayNative.SetWindowPos(
                _windowHandle,
                TrayNative.HwndTopMost,
                bounds.Left,
                bounds.Top,
                width,
                height,
                TrayNative.SWP_NOACTIVATE | TrayNative.SWP_SHOWWINDOW);
        }

        public void Hide()
        {
            if (_windowHandle == nint.Zero)
                return;

            TrayNative.ShowWindow(_windowHandle, TrayNative.SW_HIDE);
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
                throw new InvalidOperationException("Unable to register WinCalendar clock overlay window class.");
        }

        private void CreateOverlayWindow()
        {
            _windowHandle = TrayNative.CreateWindowEx(
                TrayNative.WS_EX_TOPMOST
                | TrayNative.WS_EX_TOOLWINDOW
                | TrayNative.WS_EX_LAYERED
                | TrayNative.WS_EX_NOACTIVATE,
                _windowClassName,
                _windowClassName,
                TrayNative.WS_POPUP,
                0,
                0,
                0,
                0,
                nint.Zero,
                nint.Zero,
                _moduleHandle,
                nint.Zero);

            if (_windowHandle == nint.Zero)
                throw new InvalidOperationException("Unable to create WinCalendar clock overlay window.");

            if (!TrayNative.SetLayeredWindowAttributes(_windowHandle, 0, 1, TrayNative.LWA_ALPHA))
                throw new InvalidOperationException("Unable to configure WinCalendar clock overlay transparency.");

            TrayNative.ShowWindow(_windowHandle, TrayNative.SW_HIDE);
        }

        private nint WindowProcedure(nint hWnd, uint msg, nuint wParam, nint lParam)
        {
            return msg switch
            {
                TrayNative.WM_MOUSEACTIVATE => new nint(TrayNative.MA_NOACTIVATE),
                TrayNative.WM_NCHITTEST => new nint(TrayNative.HTCLIENT),
                TrayNative.WM_LBUTTONUP => HandleLeftButtonUp(),
                _ => TrayNative.DefWindowProc(hWnd, msg, wParam, lParam)
            };
        }

        private nint HandleLeftButtonUp()
        {
            _onClick();
            return nint.Zero;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            if (_windowHandle != nint.Zero)
            {
                TrayNative.DestroyWindow(_windowHandle);
                _windowHandle = nint.Zero;
            }

            TrayNative.UnregisterClass(_windowClassName, _moduleHandle);
        }

        private static bool IsValidBounds(TaskbarNative.RECT bounds)
        {
            return bounds.Right > bounds.Left && bounds.Bottom > bounds.Top;
        }
    }
}

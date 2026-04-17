using System;
using System.Runtime.InteropServices;
using WinCalendar.Modules.Shell.Infrastructure.Win32;

namespace WinCalendar.Modules.Shell.UI;

public sealed class TrayIconHost : IDisposable
{
    private const uint NotifyIconId = 1;
    private const uint TrayCallbackMessage = 0x8000 + 7;
    private const nuint OpenPlannerCommandId = 1001;
    private const nuint ExitCommandId = 1003;

    private readonly Action _openFullApp;
    private readonly Action _exitApplication;
    private readonly TrayNative.WndProc _windowProcedure;
    private readonly string _windowClassName;
    private readonly uint _taskbarCreatedMessage;
    private readonly nint _moduleHandle;
    private nint _windowHandle;
    private bool _isDisposed;

    public TrayIconHost(Action openFullApp, Action exitApplication)
    {
        _openFullApp = openFullApp;
        _exitApplication = exitApplication;
        _windowProcedure = WindowProcedure;
        _windowClassName = $"WinCalendar.TrayIcon.{Environment.ProcessId}";
        _taskbarCreatedMessage = TrayNative.RegisterWindowMessage("TaskbarCreated");
        _moduleHandle = TrayNative.GetModuleHandle(lpModuleName: null);

        RegisterWindowClass();
        CreateMessageWindow();
        AddNotifyIcon();
    }

    private void RegisterWindowClass()
    {
        TrayNative.WNDCLASSEX windowClass = new()
        {
            cbSize = (uint)Marshal.SizeOf<TrayNative.WNDCLASSEX>(),
            lpfnWndProc = _windowProcedure,
            hInstance = _moduleHandle,
            hIcon = TrayNative.LoadIcon(nint.Zero, new nint(TrayNative.IDI_APPLICATION)),
            lpszClassName = _windowClassName
        };

        ushort classAtom = TrayNative.RegisterClassEx(ref windowClass);
        if (classAtom == 0)
            throw new InvalidOperationException("Unable to register WinCalendar tray window class.");
    }

    private void CreateMessageWindow()
    {
        _windowHandle = TrayNative.CreateWindowEx(
            dwExStyle: 0,
            lpClassName: _windowClassName,
            lpWindowName: _windowClassName,
            dwStyle: 0,
            x: 0,
            y: 0,
            nWidth: 0,
            nHeight: 0,
            hWndParent: TrayNative.HwndMessage,
            hMenu: nint.Zero,
            hInstance: _moduleHandle,
            lpParam: nint.Zero);

        if (_windowHandle == nint.Zero)
            throw new InvalidOperationException("Unable to create WinCalendar tray message window.");
    }

    private void AddNotifyIcon()
    {
        TrayNative.NOTIFYICONDATA notifyIconData = CreateNotifyIconData();
        TrayNative.Shell_NotifyIcon(TrayNative.NIM_ADD, ref notifyIconData);
        notifyIconData.uVersion = TrayNative.NOTIFYICON_VERSION_4;
        TrayNative.Shell_NotifyIcon(TrayNative.NIM_SETVERSION, ref notifyIconData);
    }

    private TrayNative.NOTIFYICONDATA CreateNotifyIconData() => new()
    {
        cbSize = (uint)Marshal.SizeOf<TrayNative.NOTIFYICONDATA>(),
        hWnd = _windowHandle,
        uID = NotifyIconId,
        uFlags = TrayNative.NIF_MESSAGE | TrayNative.NIF_ICON | TrayNative.NIF_TIP,
        uCallbackMessage = TrayCallbackMessage,
        hIcon = TrayNative.LoadIcon(nint.Zero, new nint(TrayNative.IDI_APPLICATION)),
        szTip = "WinCalendar",
        szInfo = string.Empty,
        szInfoTitle = string.Empty
    };

    private nint WindowProcedure(nint hWnd, uint msg, nuint wParam, nint lParam)
    {
        if (msg == _taskbarCreatedMessage)
        {
            AddNotifyIcon();
            return nint.Zero;
        }

        if (msg == TrayCallbackMessage)
            return HandleTrayMessage(hWnd, (uint)lParam);

        if (msg == TrayNative.WM_COMMAND)
            return HandleMenuCommand(wParam);

        return TrayNative.DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private nint HandleTrayMessage(nint hWnd, uint notificationMessage)
    {
        switch (notificationMessage)
        {
            case TrayNative.WM_LBUTTONUP:
            case TrayNative.WM_LBUTTONDBLCLK:
                _openFullApp();
                return nint.Zero;
            case TrayNative.WM_RBUTTONUP:
            case TrayNative.WM_CONTEXTMENU:
                ShowContextMenu(hWnd);
                return nint.Zero;
            default:
                return nint.Zero;
        }
    }

    private nint HandleMenuCommand(nuint wParam)
    {
        nuint commandId = wParam & 0xFFFF;

        switch (commandId)
        {
            case OpenPlannerCommandId:
                _openFullApp();
                return nint.Zero;
            case ExitCommandId:
                _exitApplication();
                return nint.Zero;
            default:
                return nint.Zero;
        }
    }

    private void ShowContextMenu(nint hWnd)
    {
        nint menuHandle = TrayNative.CreatePopupMenu();
        if (menuHandle == nint.Zero)
            return;

        try
        {
            TrayNative.AppendMenu(menuHandle, TrayNative.MF_STRING, OpenPlannerCommandId, "Open Planner");
            TrayNative.AppendMenu(menuHandle, TrayNative.MF_SEPARATOR, 0, lpNewItem: null);
            TrayNative.AppendMenu(menuHandle, TrayNative.MF_STRING, ExitCommandId, "Exit");

            if (!TrayNative.GetCursorPos(out TrayNative.POINT cursorPoint))
                return;

            TrayNative.SetForegroundWindow(hWnd);
            TrayNative.TrackPopupMenu(
                menuHandle,
                TrayNative.TPM_LEFTALIGN | TrayNative.TPM_BOTTOMALIGN | TrayNative.TPM_RIGHTBUTTON,
                cursorPoint.X,
                cursorPoint.Y,
                nReserved: 0,
                hWnd,
                prcRect: nint.Zero);
            TrayNative.PostMessage(hWnd, TrayNative.WM_NULL, 0, nint.Zero);
        }
        finally
        {
            TrayNative.DestroyMenu(menuHandle);
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        if (_windowHandle != nint.Zero)
        {
            TrayNative.NOTIFYICONDATA notifyIconData = CreateNotifyIconData();
            TrayNative.Shell_NotifyIcon(TrayNative.NIM_DELETE, ref notifyIconData);
            TrayNative.DestroyWindow(_windowHandle);
            _windowHandle = nint.Zero;
        }

        TrayNative.UnregisterClass(_windowClassName, _moduleHandle);
    }
}

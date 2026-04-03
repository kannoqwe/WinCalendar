using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.UI.Dispatching;

namespace WinCalendar.Modules.Shell.Infrastructure.Win32;

internal sealed class TaskbarClockClickInterceptor : IDisposable
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Action _onClockClick;
    private readonly LowLevelMouseHookNative.LowLevelMouseProc _hookProcedure;
    private nint _hookHandle;
    private bool _isPendingClick;
    private bool _isDisposed;

    public TaskbarClockClickInterceptor(
        DispatcherQueue dispatcherQueue,
        Action onClockClick)
    {
        _dispatcherQueue = dispatcherQueue;
        _onClockClick = onClockClick;
        _hookProcedure = HookProcedure;
        InstallHook();
    }

    private void InstallHook()
    {
        nint moduleHandle = TrayNative.GetModuleHandle(lpModuleName: null);
        _hookHandle = LowLevelMouseHookNative.SetWindowsHookEx(
            LowLevelMouseHookNative.WH_MOUSE_LL,
            _hookProcedure,
            moduleHandle,
            dwThreadId: 0);

        if (_hookHandle == nint.Zero)
            throw new InvalidOperationException("Unable to install WinCalendar clock click hook.");
    }

    private nint HookProcedure(int nCode, nuint wParam, nint lParam)
    {
        if (nCode < 0)
            return LowLevelMouseHookNative.CallNextHookEx(_hookHandle, nCode, wParam, lParam);

        LowLevelMouseHookNative.MSLLHOOKSTRUCT hookStruct =
            Marshal.PtrToStructure<LowLevelMouseHookNative.MSLLHOOKSTRUCT>(lParam);

        switch ((uint)wParam)
        {
            case LowLevelMouseHookNative.WM_LBUTTONDOWN:
                if (IsSystemClockWindowAtPoint(hookStruct.pt))
                {
                    _isPendingClick = true;
                    return 1;
                }

                break;

            case LowLevelMouseHookNative.WM_LBUTTONUP:
                if (_isPendingClick)
                {
                    _isPendingClick = false;

                    if (IsSystemClockWindowAtPoint(hookStruct.pt))
                        _dispatcherQueue.TryEnqueue(() => _onClockClick());

                    return 1;
                }

                break;
        }

        return LowLevelMouseHookNative.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private static bool IsSystemClockWindowAtPoint(LowLevelMouseHookNative.POINT point)
    {
        nint windowHandle = TaskbarNative.WindowFromPoint(point);
        while (windowHandle != nint.Zero)
        {
            if (WindowHasClass(windowHandle, "TrayClockWClass"))
                return true;

            windowHandle = TaskbarNative.GetParent(windowHandle);
        }

        return false;
    }

    private static bool WindowHasClass(nint windowHandle, string className)
    {
        StringBuilder classNameBuffer = new(256);
        int copiedLength = TaskbarNative.GetClassName(windowHandle, classNameBuffer, classNameBuffer.Capacity);
        return copiedLength > 0 && string.Equals(classNameBuffer.ToString(), className, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        if (_hookHandle != nint.Zero)
        {
            LowLevelMouseHookNative.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = nint.Zero;
        }
    }
}

using System.Collections.Generic;
using System.Text;

namespace WinCalendar.Modules.Shell.Infrastructure.Win32;

internal sealed class TaskbarClockBoundsProvider
{
    private const string PrimaryTaskbarClassName = "Shell_TrayWnd";
    private const string SecondaryTaskbarClassName = "Shell_SecondaryTrayWnd";
    private const string ClockClassName = "TrayClockWClass";

    public IReadOnlyList<TaskbarNative.RECT> GetClockBounds()
    {
        List<TaskbarNative.RECT> bounds = [];

        AddClockBoundsForPrimaryTaskbar(bounds);
        AddClockBoundsForSecondaryTaskbars(bounds);

        if (bounds.Count == 0 && TryGetFallbackBounds(out TaskbarNative.RECT fallbackBounds))
            bounds.Add(fallbackBounds);

        return bounds;
    }

    private static void AddClockBoundsForPrimaryTaskbar(List<TaskbarNative.RECT> bounds)
    {
        nint taskbarWindow = TaskbarNative.FindWindow(PrimaryTaskbarClassName, lpWindowName: null);
        if (taskbarWindow != nint.Zero)
            AddClockBoundsForTaskbarWindow(taskbarWindow, bounds);
    }

    private static void AddClockBoundsForSecondaryTaskbars(List<TaskbarNative.RECT> bounds)
    {
        nint previousWindow = nint.Zero;

        while (true)
        {
            nint secondaryTaskbarWindow = TaskbarNative.FindWindowEx(
                nint.Zero,
                previousWindow,
                SecondaryTaskbarClassName,
                lpszWindow: null);

            if (secondaryTaskbarWindow == nint.Zero)
                return;

            AddClockBoundsForTaskbarWindow(secondaryTaskbarWindow, bounds);
            previousWindow = secondaryTaskbarWindow;
        }
    }

    private static void AddClockBoundsForTaskbarWindow(nint taskbarWindow, List<TaskbarNative.RECT> bounds)
    {
        nint clockWindow = FindDescendantWindow(taskbarWindow, ClockClassName);
        if (clockWindow == nint.Zero)
            return;

        if (!TaskbarNative.GetWindowRect(clockWindow, out TaskbarNative.RECT clockBounds))
            return;

        if (!IsValidBounds(clockBounds))
            return;

        if (ContainsBounds(bounds, clockBounds))
            return;

        bounds.Add(clockBounds);
    }

    private static nint FindDescendantWindow(nint parentWindow, string className)
    {
        nint childWindow = nint.Zero;

        while (true)
        {
            childWindow = TaskbarNative.FindWindowEx(
                parentWindow,
                childWindow,
                lpszClass: null,
                lpszWindow: null);

            if (childWindow == nint.Zero)
                return nint.Zero;

            if (WindowHasClass(childWindow, className))
                return childWindow;

            nint descendantWindow = FindDescendantWindow(childWindow, className);
            if (descendantWindow != nint.Zero)
                return descendantWindow;
        }
    }

    private static bool WindowHasClass(nint windowHandle, string className)
    {
        StringBuilder classNameBuffer = new(256);
        int copiedLength = TaskbarNative.GetClassName(windowHandle, classNameBuffer, classNameBuffer.Capacity);
        return copiedLength > 0 && classNameBuffer.ToString() == className;
    }

    private static bool TryGetFallbackBounds(out TaskbarNative.RECT bounds)
    {
        bounds = default;

        nint taskbarWindow = TaskbarNative.FindWindow(PrimaryTaskbarClassName, lpWindowName: null);
        if (taskbarWindow == nint.Zero)
            return false;

        TaskbarNative.APPBARDATA appBarData = new()
        {
            cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<TaskbarNative.APPBARDATA>(),
            hWnd = taskbarWindow
        };

        TaskbarNative.SHAppBarMessage(TaskbarNative.ABM_GETTASKBARPOS, ref appBarData);
        if (appBarData.uEdge != TaskbarNative.ABE_BOTTOM || !IsValidBounds(appBarData.rc))
            return false;

        const int fallbackClockWidth = 120;
        bounds = new TaskbarNative.RECT
        {
            Left = appBarData.rc.Right - fallbackClockWidth,
            Top = appBarData.rc.Top,
            Right = appBarData.rc.Right,
            Bottom = appBarData.rc.Bottom
        };

        return IsValidBounds(bounds);
    }

    private static bool IsValidBounds(TaskbarNative.RECT bounds)
    {
        return bounds.Right > bounds.Left && bounds.Bottom > bounds.Top;
    }

    private static bool ContainsBounds(List<TaskbarNative.RECT> bounds, TaskbarNative.RECT candidate)
    {
        foreach (TaskbarNative.RECT current in bounds)
        {
            if (current.Left == candidate.Left
                && current.Top == candidate.Top
                && current.Right == candidate.Right
                && current.Bottom == candidate.Bottom)
            {
                return true;
            }
        }

        return false;
    }
}

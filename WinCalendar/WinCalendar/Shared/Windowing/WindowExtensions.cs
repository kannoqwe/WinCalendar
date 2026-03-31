using System;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace WinCalendar.Shared.Windowing;

internal static class WindowExtensions
{
    public static AppWindow GetAppWindow(this Window window)
    {
        IntPtr handle = WindowNative.GetWindowHandle(window);
        WindowId id = Win32Interop.GetWindowIdFromWindow(handle);
        return AppWindow.GetFromWindowId(id);
    }
}

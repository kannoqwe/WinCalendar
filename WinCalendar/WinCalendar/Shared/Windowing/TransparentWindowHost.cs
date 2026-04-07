using System;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace WinCalendar.Shared.Windowing;

internal static class TransparentWindowHost
{
    public enum WindowOutlineShape
    {
        AllRounded,
        LeftRounded,
        RightRounded
    }

    private const int GwlStyle = -16;
    private const int GwlExStyle = -20;

    private const long WsBorder = 0x00800000L;
    private const long WsCaption = 0x00C00000L;
    private const long WsDlgFrame = 0x00400000L;
    private const long WsMinimizeBox = 0x00020000L;
    private const long WsMaximizeBox = 0x00010000L;
    private const long WsSysMenu = 0x00080000L;
    private const long WsThickFrame = 0x00040000L;
    private const long WsPopup = unchecked((long)0x80000000);

    private const long WsExAppWindow = 0x00040000L;
    private const long WsExToolWindow = 0x00000080L;

    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;

    private const uint DwmwaNcRenderingPolicy = 2;
    private const uint DwmwaWindowCornerPreference = 33;
    private const uint DwmwaBorderColor = 34;
    private const uint DwmwaCaptionColor = 35;
    private const uint DwmwaTextColor = 36;
    private const uint DwmColorNone = 0xFFFFFFFE;
    private const uint DwmColorTransparent = 0x00000000;

    private const uint DwmncrpDisabled = 1;
    private const uint DwmwcpDoNotRound = 1;
    private const int RgnOr = 2;

    public static void Apply(Window window, int width, int height, int cornerRadius, WindowOutlineShape outlineShape)
    {
        nint windowHandle = WindowNative.GetWindowHandle(window);
        if (windowHandle == nint.Zero)
            return;

        ConfigureNativeStyles(windowHandle);
        ConfigureTitleBar(window);
        RemoveDwmBorder(windowHandle);
        ApplyWindowRegion(windowHandle, width, height, cornerRadius, outlineShape);
    }

    private static void ConfigureNativeStyles(nint windowHandle)
    {
        long style = GetWindowLongPtr(windowHandle, GwlStyle).ToInt64();
        style &= ~(WsBorder | WsCaption | WsDlgFrame | WsMinimizeBox | WsMaximizeBox | WsSysMenu | WsThickFrame);
        style |= WsPopup;
        SetWindowLongPtr(windowHandle, GwlStyle, new nint(style));

        long exStyle = GetWindowLongPtr(windowHandle, GwlExStyle).ToInt64();
        exStyle &= ~WsExAppWindow;
        exStyle |= WsExToolWindow;
        SetWindowLongPtr(windowHandle, GwlExStyle, new nint(exStyle));

        SetWindowPos(
            windowHandle,
            nint.Zero,
            0,
            0,
            0,
            0,
            SwpNoActivate | SwpNoMove | SwpNoSize | SwpNoZOrder | SwpFrameChanged);
    }

    private static void ConfigureTitleBar(Window window)
    {
        if (!AppWindowTitleBar.IsCustomizationSupported())
            return;

        AppWindowTitleBar titleBar = window.GetAppWindow().TitleBar;
        titleBar.ExtendsContentIntoTitleBar = true;
        titleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        titleBar.ButtonForegroundColor = Colors.Transparent;
        titleBar.ButtonInactiveForegroundColor = Colors.Transparent;
        titleBar.ButtonHoverBackgroundColor = Colors.Transparent;
        titleBar.ButtonHoverForegroundColor = Colors.Transparent;
        titleBar.ButtonPressedBackgroundColor = Colors.Transparent;
        titleBar.ButtonPressedForegroundColor = Colors.Transparent;
    }

    private static void RemoveDwmBorder(nint windowHandle)
    {
        uint ncRenderingPolicy = DwmncrpDisabled;
        uint cornerPreference = DwmwcpDoNotRound;
        uint borderColor = DwmColorNone;
        uint captionColor = DwmColorNone;
        uint textColor = DwmColorTransparent;

        _ = DwmSetWindowAttribute(windowHandle, DwmwaNcRenderingPolicy, ref ncRenderingPolicy, sizeof(uint));
        _ = DwmSetWindowAttribute(windowHandle, DwmwaWindowCornerPreference, ref cornerPreference, sizeof(uint));
        _ = DwmSetWindowAttribute(windowHandle, DwmwaBorderColor, ref borderColor, sizeof(uint));
        _ = DwmSetWindowAttribute(windowHandle, DwmwaCaptionColor, ref captionColor, sizeof(uint));
        _ = DwmSetWindowAttribute(windowHandle, DwmwaTextColor, ref textColor, sizeof(uint));
    }

    private static void ApplyWindowRegion(
        nint windowHandle,
        int width,
        int height,
        int cornerRadius,
        WindowOutlineShape outlineShape)
    {
        int ellipseDiameter = Math.Max(2, cornerRadius * 2);
        nint baseRegion = CreateRoundRectRgn(0, 0, width + 1, height + 1, ellipseDiameter, ellipseDiameter);
        if (baseRegion == nint.Zero)
            return;

        nint? additiveRegion = outlineShape switch
        {
            WindowOutlineShape.LeftRounded => CreateRectRgn(cornerRadius, 0, width + 1, height + 1),
            WindowOutlineShape.RightRounded => CreateRectRgn(0, 0, Math.Max(0, width - cornerRadius), height + 1),
            _ => null
        };

        try
        {
            if (additiveRegion is not null && additiveRegion != nint.Zero)
                CombineRgn(baseRegion, baseRegion, additiveRegion.Value, RgnOr);

            SetWindowRgn(windowHandle, baseRegion, true);
            baseRegion = nint.Zero;
        }
        finally
        {
            if (additiveRegion is not null && additiveRegion != nint.Zero)
                DeleteObject(additiveRegion.Value);

            if (baseRegion != nint.Zero)
                DeleteObject(baseRegion);
        }
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        nint hWnd,
        nint hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern nint CreateRoundRectRgn(
        int nLeftRect,
        int nTopRect,
        int nRightRect,
        int nBottomRect,
        int nWidthEllipse,
        int nHeightEllipse);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern nint CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern int CombineRgn(nint hrgnDest, nint hrgnSrc1, nint hrgnSrc2, int fnCombineMode);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowRgn(nint hWnd, nint hRgn, bool bRedraw);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, uint dwAttribute, ref uint pvAttribute, int cbAttribute);

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(nint hObject);
}

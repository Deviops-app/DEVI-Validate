using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Devi.Theme;

/// <summary>
/// Window behavior shared by every DEVI window: dark DWM frame in the canvas color, rounded
/// corners on Windows 11, maximize to the work area, and a first size that fits the screen.
/// </summary>
public static class DeviWindowChrome
{
    private const int WmGetMinMaxInfo = 0x0024;
    private const uint MonitorDefaultToNearest = 2;
    private const int DwmUseImmersiveDarkMode = 20;
    private const int DwmWindowCornerPreference = 33;
    private const int DwmBorderColor = 34;
    private const int DwmCaptionColor = 35;
    private const int DwmTextColor = 36;
    private const int DwmCornerRound = 2;

    /// <summary>Call from OnSourceInitialized.</summary>
    public static void Attach(Window window)
    {
        if (PresentationSource.FromVisual(window) is not HwndSource source)
        {
            return;
        }

        source.AddHook(WindowProc);
        ApplyFrame(source.Handle);
    }

    /// <summary>Sizes the window to the preferred size, or the work area if smaller, and centers it.</summary>
    public static void FitToWorkArea(Window window, double preferredWidth, double preferredHeight)
    {
        var area = SystemParameters.WorkArea;
        const double margin = 16d;
        var availableWidth = Math.Max(320d, area.Width - margin);
        var availableHeight = Math.Max(240d, area.Height - margin);
        if (window.MinWidth > availableWidth)
        {
            window.MinWidth = availableWidth;
        }

        if (window.MinHeight > availableHeight)
        {
            window.MinHeight = availableHeight;
        }

        var width = Math.Min(preferredWidth, availableWidth);
        var height = Math.Min(preferredHeight, availableHeight);
        window.Width = width;
        window.Height = height;
        window.Left = area.Left + ((area.Width - width) / 2d);
        window.Top = area.Top + ((area.Height - height) / 2d);
    }

    private static void ApplyFrame(IntPtr hwnd)
    {
        var dark = 1;
        _ = DwmSetWindowAttribute(hwnd, DwmUseImmersiveDarkMode, ref dark, sizeof(int));
        var round = DwmCornerRound;
        _ = DwmSetWindowAttribute(hwnd, DwmWindowCornerPreference, ref round, sizeof(int));
        var frame = ColorRef(DeviTheme.Color("CanvasColor"));
        _ = DwmSetWindowAttribute(hwnd, DwmBorderColor, ref frame, sizeof(int));
        _ = DwmSetWindowAttribute(hwnd, DwmCaptionColor, ref frame, sizeof(int));
        var text = ColorRef(DeviTheme.Color("TextColor"));
        _ = DwmSetWindowAttribute(hwnd, DwmTextColor, ref text, sizeof(int));
    }

    /// <summary>COLORREF is 0x00BBGGRR.</summary>
    private static int ColorRef(Color color) => color.R | (color.G << 8) | (color.B << 16);

    private static IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmGetMinMaxInfo)
        {
            ApplyWorkArea(hwnd, lParam);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static void ApplyWorkArea(IntPtr hwnd, IntPtr lParam)
    {
        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        var monitorInfo = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref monitorInfo))
        {
            return;
        }

        var info = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        var work = monitorInfo.rcWork;
        var bounds = monitorInfo.rcMonitor;
        info.ptMaxPosition.x = work.Left - bounds.Left;
        info.ptMaxPosition.y = work.Top - bounds.Top;
        info.ptMaxSize.x = work.Right - work.Left;
        info.ptMaxSize.y = work.Bottom - work.Top;
        info.ptMaxTrackSize = info.ptMaxSize;
        Marshal.StructureToPtr(info, lParam, true);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo info);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public Point ptReserved;
        public Point ptMaxSize;
        public Point ptMaxPosition;
        public Point ptMinTrackSize;
        public Point ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int cbSize;
        public Rect rcMonitor;
        public Rect rcWork;
        public int dwFlags;
    }
}

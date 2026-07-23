using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace DmdClock.App.Screensaver;

internal static class ScreenSaverPreviewHost
{
    private const int StyleIndex = -16;
    private const long ChildStyle = 0x40000000;
    private const long PopupStyle = 0x80000000;

    public static bool Attach(Window window, nint parentHandle)
    {
        if (!OperatingSystem.IsWindows() || parentHandle == 0 || !IsWindow(parentHandle)) return false;
        var platformHandle = window.TryGetPlatformHandle()?.Handle ?? 0;
        if (platformHandle == 0) return false;
        var style = GetWindowLongPtr(platformHandle, StyleIndex);
        SetWindowLongPtr(platformHandle, StyleIndex, (nint)(((long)style & ~PopupStyle) | ChildStyle));
        var previousParent = SetParent(platformHandle, parentHandle);
        if (previousParent == 0 && Marshal.GetLastWin32Error() != 0) return false;
        if (!GetClientRect(parentHandle, out var bounds)) return false;
        return MoveWindow(platformHandle, 0, 0, bounds.Right, bounds.Bottom, true);
    }

    public static bool ParentExists(nint parentHandle) =>
        !OperatingSystem.IsWindows() || parentHandle != 0 && IsWindow(parentHandle);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetParent(nint child, nint newParent);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetClientRect(nint window, out Rect bounds);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool MoveWindow(nint window, int x, int y, int width, int height, bool repaint);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(nint window);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint window, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint window, int index, nint newValue);
}

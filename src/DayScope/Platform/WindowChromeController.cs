using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DayScope.Platform;

/// <summary>
/// Applies native Windows title-bar attributes for DayScope windows.
/// </summary>
public sealed class WindowChromeController : IWindowChromeController
{
    /// <inheritdoc />
    public void ApplyTitleBarTheme(Window window, bool useDarkChrome)
    {
        ArgumentNullException.ThrowIfNull(window);

        var windowHandle = new WindowInteropHelper(window).Handle;
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        var enabled = useDarkChrome ? 1 : 0;
        _ = DwmSetWindowAttribute(
            windowHandle,
            DWMWA_USE_IMMERSIVE_DARK_MODE,
            ref enabled,
            sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
}

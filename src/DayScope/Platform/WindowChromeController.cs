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
    public void ApplyTitleBarTheme(Window window, bool useDarkChrome, bool useGlassBackdrop)
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

        var useHostBackdropBrush = useGlassBackdrop ? 1 : 0;
        _ = DwmSetWindowAttribute(
            windowHandle,
            DWMWA_USE_HOSTBACKDROPBRUSH,
            ref useHostBackdropBrush,
            sizeof(int));

        var useRedirectionBitmapAlpha = useGlassBackdrop ? 1 : 0;
        _ = DwmSetWindowAttribute(
            windowHandle,
            DWMWA_REDIRECTIONBITMAP_ALPHA,
            ref useRedirectionBitmapAlpha,
            sizeof(int));

        if (HwndSource.FromHwnd(windowHandle) is { CompositionTarget: { } compositionTarget })
        {
            compositionTarget.BackgroundColor = useGlassBackdrop
                ? System.Windows.Media.Color.FromArgb(0, 0, 0, 0)
                : System.Windows.Media.Color.FromArgb(255, 0, 0, 0);
        }

        var margins = useGlassBackdrop
            ? new Margins(-1)
            : new Margins(0);
        _ = DwmExtendFrameIntoClientArea(windowHandle, ref margins);

        var backdropType = useGlassBackdrop
            ? (int)DwmSystemBackdropType.TransientWindow
            : (int)DwmSystemBackdropType.None;
        _ = DwmSetWindowAttribute(
            windowHandle,
            DWMWA_SYSTEMBACKDROP_TYPE,
            ref backdropType,
            sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(
        IntPtr hwnd,
        ref Margins margins);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_USE_HOSTBACKDROPBRUSH = 17;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    private const int DWMWA_REDIRECTIONBITMAP_ALPHA = 52;

    private enum DwmSystemBackdropType
    {
        Auto = 0,
        None = 1,
        MainWindow = 2,
        TransientWindow = 3,
        TabbedWindow = 4
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct Margins
    {
        public Margins(int uniformSize)
        {
            _leftWidth = uniformSize;
            _rightWidth = uniformSize;
            _topHeight = uniformSize;
            _bottomHeight = uniformSize;
        }

        private readonly int _leftWidth;
        private readonly int _rightWidth;
        private readonly int _topHeight;
        private readonly int _bottomHeight;
    }
}

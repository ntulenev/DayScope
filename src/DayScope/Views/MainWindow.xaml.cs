using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Diagnostics;

using Microsoft.Extensions.Options;

using DayScope.Domain.Configuration;
using DayScope.Themes;
using DayScope.ViewModels;

namespace DayScope.Views;

/// <summary>
/// Hosts the main DayScope dashboard window and its window-specific interactions.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The dashboard view model bound to the window.</param>
    /// <param name="themeManager">The theme manager used to update window chrome.</param>
    /// <param name="windowOptions">The configured window sizing options.</param>
    public MainWindow(
        MainWindowViewModel viewModel,
        ThemeManager themeManager,
        IOptions<WindowSettings> windowOptions)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(themeManager);
        ArgumentNullException.ThrowIfNull(windowOptions);

        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _themeManager = themeManager;
        ApplyWindowSettings(windowOptions.Value);
        SourceInitialized += (_, _) => ApplyWindowTitleBarTheme();
        SizeChanged += (_, _) => UpdateScheduleWidth();
        Closing += OnClosing;
        Closed += (_, _) =>
        {
            _themeManager.ThemeChanged -= OnThemeChanged;
            _viewModel.Dispose();
        };
        _themeManager.ThemeChanged += OnThemeChanged;
    }

    /// <summary>
    /// Performs initial window sizing and loads dashboard data.
    /// </summary>
    /// <returns>A task that completes when the window is initialized.</returns>
    public async Task InitializeAsync()
    {
        UpdateScheduleWidth();
        await _viewModel.InitializeAsync();
    }

    /// <summary>
    /// Shows the window from the tray and restores focus to it.
    /// </summary>
    public void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
        Focus();

        UpdateLayout();
        UpdateScheduleWidth();
        ScrollScheduleToNowLine();
    }

    /// <summary>
    /// Hides the window to the system tray.
    /// </summary>
    public void HideToTray()
    {
        Hide();
    }

    /// <summary>
    /// Requests an immediate dashboard refresh.
    /// </summary>
    /// <returns>A task that completes when the refresh has finished.</returns>
    public Task RefreshNowAsync() => _viewModel.RefreshNowAsync();

    /// <summary>
    /// Allows the window to close instead of minimizing to the tray.
    /// </summary>
    public void CloseFromTray()
    {
        _allowClose = true;
        Close();
    }

    /// <summary>
    /// Opens details when the user clicks an event card.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The mouse event arguments.</param>
    private void OnEventCardMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        _viewModel.OpenEventDetails(element.DataContext);
        e.Handled = true;
    }

    /// <summary>
    /// Closes the details overlay when the background is clicked.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The mouse event arguments.</param>
    private void OnEventDetailsOverlayMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _viewModel.CloseEventDetails();
        e.Handled = true;
    }

    /// <summary>
    /// Closes the event details overlay.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnCloseEventDetailsClick(object sender, RoutedEventArgs e)
    {
        _viewModel.CloseEventDetails();
    }

    /// <summary>
    /// Opens the selected event link in the default browser.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnOpenEventLinkClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedEventDetails?.JoinUrl is not Uri joinUrl)
        {
            return;
        }

        OpenUri(joinUrl);
    }

    /// <summary>
    /// Opens Gmail for the current inbox context.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnOpenUnreadEmailsClick(object sender, RoutedEventArgs e)
    {
        OpenUri(_viewModel.UnreadEmailInboxUri);
    }

    /// <summary>
    /// Opens Google Calendar for the currently selected day.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnOpenGoogleCalendarClick(object sender, RoutedEventArgs e)
    {
        OpenUri(_viewModel.GoogleCalendarUri);
    }

    /// <summary>
    /// Navigates to the previous day.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private async void OnPreviousDayClickAsync(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateDaysAsync(-1);
        ScrollScheduleToNowLine();
    }

    /// <summary>
    /// Navigates to the next day.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private async void OnNextDayClickAsync(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateDaysAsync(1);
        ScrollScheduleToNowLine();
    }

    /// <summary>
    /// Copies the selected event link to the clipboard.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnCopyEventLinkClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedEventDetails?.JoinUrl is not Uri joinUrl)
        {
            return;
        }

        try
        {
            System.Windows.Clipboard.SetText(joinUrl.AbsoluteUri);
        }
        catch (COMException)
        {
            // Ignore clipboard access failures and keep the dialog open.
        }
        catch (ExternalException)
        {
            // Ignore clipboard access failures and keep the dialog open.
        }
    }

    /// <summary>
    /// Applies configured size constraints to the window.
    /// </summary>
    /// <param name="settings">The window settings to apply.</param>
    private void ApplyWindowSettings(WindowSettings settings)
    {
        Width = settings.Width;
        Height = settings.Height;
        MinWidth = settings.MinWidth;
        MinHeight = settings.MinHeight;
    }

    /// <summary>
    /// Recalculates the available width for the schedule surface.
    /// </summary>
    private void UpdateScheduleWidth()
    {
        var availableWidth = ScheduleSurfaceBorder.ActualWidth > 0
            ? ScheduleSurfaceBorder.ActualWidth - 18
            : ActualWidth - 280;

        _viewModel.UpdateAvailableScheduleWidth(availableWidth);
    }

    /// <summary>
    /// Scrolls the schedule so the current-time marker is visible.
    /// </summary>
    private void ScrollScheduleToNowLine()
    {
        var targetOffset = Math.Max(0, _viewModel.NowLineTop - 280);
        ScheduleScrollViewer.ScrollToVerticalOffset(targetOffset);
    }

    /// <summary>
    /// Hides the window to the tray unless tray-driven close is allowed.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The cancel event arguments.</param>
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        HideToTray();
    }

    /// <summary>
    /// Reapplies title bar chrome when the theme changes.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnThemeChanged(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(ApplyWindowTitleBarTheme);
    }

    /// <summary>
    /// Applies the dark-mode title bar preference to the native window handle.
    /// </summary>
    private void ApplyWindowTitleBarTheme()
    {
        var windowHandle = new WindowInteropHelper(this).Handle;
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        var enabled = _themeManager.IsDarkTheme ? 1 : 0;
        _ = DwmSetWindowAttribute(
            windowHandle,
            DWMWA_USE_IMMERSIVE_DARK_MODE,
            ref enabled,
            sizeof(int));
    }

    /// <summary>
    /// Opens a URI through the system shell.
    /// </summary>
    /// <param name="uri">The URI to open.</param>
    private static void OpenUri(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri)
            {
                UseShellExecute = true
            });
        }
        catch (InvalidOperationException)
        {
            // Ignore shell launch failures.
        }
        catch (Win32Exception)
        {
            // Ignore shell launch failures.
        }
    }

    /// <summary>
    /// Sets a DWM window attribute for the native window handle.
    /// </summary>
    /// <param name="hwnd">The target window handle.</param>
    /// <param name="dwAttribute">The DWM attribute identifier.</param>
    /// <param name="pvAttribute">The attribute value.</param>
    /// <param name="cbAttribute">The size of the attribute value.</param>
    /// <returns>The native HRESULT.</returns>
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    private bool _allowClose;
    private readonly ThemeManager _themeManager;
    private readonly MainWindowViewModel _viewModel;
}
